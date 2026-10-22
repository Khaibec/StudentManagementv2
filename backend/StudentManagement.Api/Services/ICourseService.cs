using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Api.Data;
using StudentManagement.Api.DTOs;
using StudentManagement.Api.Entities;

namespace StudentManagement.Api.Services
{
    public interface ICourseService
    {
        Task<List<CourseDto>> GetCoursesAsync();
        Task<(bool Success, int StatusCode, string Error, CourseDto Data)> GetCourseByIdAsync(int id);
        Task<(bool Success, int StatusCode, string Error, CourseDto Data)> CreateCourseAsync(CreateCourseRequest request);
        Task<(bool Success, int StatusCode, string Error, CourseDto Data)> UpdateCourseAsync(int id, UpdateCourseRequest request);
        Task<(bool Success, int StatusCode, string Error)> DeleteCourseAsync(int id);
    }

    public class CourseService : ICourseService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CourseService(AppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<CourseDto>> GetCoursesAsync()
        {
            var userId = _currentUserService.UserId;
            var role = _currentUserService.Role;

            IQueryable<Course> query = _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .Include(c => c.Enrollments);

            if (_currentUserService.IsStudent && userId.HasValue)
            {
                // Student: only enrolled courses
                query = query.Where(c => c.Enrollments.Any(e => e.StudentId == userId.Value));
            }
            else if (_currentUserService.IsTeacher && userId.HasValue)
            {
                // Teacher: only assigned courses
                query = query.Where(c => c.TeacherId == userId.Value);
            }
            else if (_currentUserService.IsAdmin)
            {
                // Admin: all courses
            }
            else
            {
                // Unknown/unauthorized role: return empty list
                return new List<CourseDto>();
            }

            var courses = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return courses.Select(c => new CourseDto
            {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                Description = c.Description,
                TeacherId = c.TeacherId,
                TeacherName = c.Teacher?.FullName ?? "Chưa phân công",
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsEnrolled = userId.HasValue && c.Enrollments.Any(e => e.StudentId == userId.Value),
                EnrollmentCount = c.Enrollments.Count
            }).ToList();
        }

        public async Task<(bool Success, int StatusCode, string Error, CourseDto Data)> GetCourseByIdAsync(int id)
        {
            var userId = _currentUserService.UserId;

            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return (false, 404, "Không tìm thấy khóa học", null);
            }

            // Authorization check
            if (_currentUserService.IsStudent && userId.HasValue)
            {
                var isEnrolled = course.Enrollments.Any(e => e.StudentId == userId.Value);
                if (!isEnrolled)
                {
                    return (false, 403, "Bạn không có quyền truy cập khóa học này", null);
                }
            }
            else if (_currentUserService.IsTeacher && userId.HasValue)
            {
                if (course.TeacherId != userId.Value)
                {
                    return (false, 403, "Bạn không có quyền quản lý khóa học này", null);
                }
            }
            else if (!_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền truy cập khóa học này", null);
            }

            var dto = new CourseDto
            {
                Id = course.Id,
                Code = course.Code,
                Title = course.Title,
                Description = course.Description,
                TeacherId = course.TeacherId,
                TeacherName = course.Teacher?.FullName ?? "Chưa phân công",
                Status = course.Status,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                IsEnrolled = userId.HasValue && course.Enrollments.Any(e => e.StudentId == userId.Value),
                EnrollmentCount = course.Enrollments.Count
            };

            return (true, 200, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, CourseDto Data)> CreateCourseAsync(CreateCourseRequest request)
        {
            var userId = _currentUserService.UserId;

            if (!_currentUserService.IsAdmin && !_currentUserService.IsTeacher)
            {
                return (false, 403, "Chỉ Giảng viên hoặc Quản trị viên mới có quyền tạo khóa học", null);
            }

            // Check duplicate course code
            var codeExists = await _context.Courses.AnyAsync(c => c.Code == request.Code);
            if (codeExists)
            {
                return (false, 400, "Mã khóa học đã tồn tại trong hệ thống", null);
            }

            int teacherId;
            if (_currentUserService.IsTeacher)
            {
                teacherId = userId.Value;
            }
            else // Admin
            {
                if (request.TeacherId.HasValue)
                {
                    var teacher = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.TeacherId.Value && u.Role == UserRole.Teacher);
                    if (teacher == null)
                    {
                        return (false, 400, "Giảng viên được chỉ định không hợp lệ", null);
                    }
                    teacherId = teacher.Id;
                }
                else
                {
                    teacherId = userId.Value;
                }
            }

            var course = new Course
            {
                Code = request.Code.Trim(),
                Title = request.Title.Trim(),
                Description = request.Description,
                TeacherId = teacherId,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Load teacher info
            await _context.Entry(course).Reference(c => c.Teacher).LoadAsync();

            var dto = new CourseDto
            {
                Id = course.Id,
                Code = course.Code,
                Title = course.Title,
                Description = course.Description,
                TeacherId = course.TeacherId,
                TeacherName = course.Teacher?.FullName ?? "Chưa phân công",
                Status = course.Status,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                IsEnrolled = false,
                EnrollmentCount = 0
            };

            return (true, 201, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, CourseDto Data)> UpdateCourseAsync(int id, UpdateCourseRequest request)
        {
            var userId = _currentUserService.UserId;

            var course = await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return (false, 404, "Không tìm thấy khóa học", null);
            }

            // Ownership check
            if (_currentUserService.IsTeacher && course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền chỉnh sửa khóa học của giảng viên khác", null);
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này", null);
            }

            course.Title = request.Title.Trim();
            course.Description = request.Description;
            course.Status = request.Status;
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var dto = new CourseDto
            {
                Id = course.Id,
                Code = course.Code,
                Title = course.Title,
                Description = course.Description,
                TeacherId = course.TeacherId,
                TeacherName = course.Teacher?.FullName ?? "Chưa phân công",
                Status = course.Status,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                IsEnrolled = userId.HasValue && course.Enrollments.Any(e => e.StudentId == userId.Value),
                EnrollmentCount = course.Enrollments.Count
            };

            return (true, 200, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error)> DeleteCourseAsync(int id)
        {
            var userId = _currentUserService.UserId;

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null)
            {
                return (false, 404, "Không tìm thấy khóa học");
            }

            // Ownership check
            if (_currentUserService.IsTeacher && course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền xóa khóa học của giảng viên khác");
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này");
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return (true, 204, null);
        }
    }
}
