using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudentManagement.Api.Data;
using StudentManagement.Api.DTOs;
using StudentManagement.Api.Entities;

namespace StudentManagement.Api.Services
{
    public interface IAssignmentService
    {
        // Assignments
        Task<(bool Success, int StatusCode, string Error, List<AssignmentDto> Data)> GetCourseAssignmentsAsync(int courseId);
        Task<(bool Success, int StatusCode, string Error, AssignmentDetailDto Data)> GetAssignmentByIdAsync(int assignmentId);
        Task<(bool Success, int StatusCode, string Error, AssignmentDto Data)> CreateAssignmentAsync(int courseId, CreateAssignmentRequest request);
        Task<(bool Success, int StatusCode, string Error, AssignmentDto Data)> UpdateAssignmentAsync(int assignmentId, UpdateAssignmentRequest request);
        Task<(bool Success, int StatusCode, string Error)> DeleteAssignmentAsync(int assignmentId);

        // Submissions
        Task<(bool Success, int StatusCode, string Error, SubmissionDto Data)> SubmitAssignmentAsync(int assignmentId, string textContent, IFormFile file);
        Task<(bool Success, int StatusCode, string Error, SubmissionDto Data)> GetMySubmissionAsync(int assignmentId);
        Task<(bool Success, int StatusCode, string Error, List<SubmissionDto> Data)> GetAssignmentSubmissionsAsync(int assignmentId);
        Task<(bool Success, int StatusCode, string Error, SubmissionDto Data)> GradeSubmissionAsync(int submissionId, GradeSubmissionRequest request);
        Task<(bool Success, int StatusCode, string Error, Stream FileStream, string ContentType, string FileName)> DownloadSubmissionFileAsync(int submissionId);
    }

    public class AssignmentService : IAssignmentService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly string _storageRoot;

        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx", ".doc", ".pptx", ".ppt", ".xlsx", ".xls", ".txt", ".zip", ".rar", ".7z", ".png", ".jpg", ".jpeg"
        };

        private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

        public AssignmentService(AppDbContext context, ICurrentUserService currentUserService, IConfiguration configuration)
        {
            _context = context;
            _currentUserService = currentUserService;
            var relativeStorage = configuration.GetValue<string>("Storage:RootPath") ?? "uploads";
            _storageRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeStorage, "submissions"));
            if (!Directory.Exists(_storageRoot))
            {
                Directory.CreateDirectory(_storageRoot);
            }
        }

        // ==================== ASSIGNMENTS ====================

        public async Task<(bool Success, int StatusCode, string Error, List<AssignmentDto> Data)> GetCourseAssignmentsAsync(int courseId)
        {
            var userId = _currentUserService.UserId;

            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return (false, 404, "Không tìm thấy khóa học", null);
            }

            // Scope check
            if (_currentUserService.IsStudent && userId.HasValue)
            {
                if (!course.Enrollments.Any(e => e.StudentId == userId.Value))
                {
                    return (false, 403, "Bạn không có quyền truy cập bài tập của khóa học này", null);
                }
            }
            else if (_currentUserService.IsTeacher && userId.HasValue)
            {
                if (course.TeacherId != userId.Value)
                {
                    return (false, 403, "Bạn không có quyền xem bài tập ngoài khóa học được phân công", null);
                }
            }
            else if (!_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền truy cập bài tập", null);
            }

            var assignments = await _context.Assignments
                .AsNoTracking()
                .Where(a => a.CourseId == courseId)
                .OrderBy(a => a.DueDate)
                .Include(a => a.Submissions)
                .ToListAsync();

            var now = DateTime.UtcNow;

            var result = assignments.Select(a =>
            {
                var mySubmission = userId.HasValue
                    ? a.Submissions.FirstOrDefault(s => s.StudentId == userId.Value)
                    : null;

                return new AssignmentDto
                {
                    Id = a.Id,
                    CourseId = a.CourseId,
                    Title = a.Title,
                    Description = a.Description,
                    DueDate = a.DueDate,
                    MaxScore = a.MaxScore,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    IsOverdue = now > a.DueDate,
                    HasSubmitted = mySubmission != null,
                    SubmissionStatus = mySubmission != null ? mySubmission.Status : "NOT_SUBMITTED",
                    Score = mySubmission?.Score,
                    IsGraded = mySubmission != null && mySubmission.Score.HasValue,
                    SubmissionCount = a.Submissions.Count,
                    GradedCount = a.Submissions.Count(s => s.Score.HasValue)
                };
            }).ToList();

            return (true, 200, null, result);
        }

        public async Task<(bool Success, int StatusCode, string Error, AssignmentDetailDto Data)> GetAssignmentByIdAsync(int assignmentId)
        {
            var userId = _currentUserService.UserId;

            var assignment = await _context.Assignments
                .AsNoTracking()
                .Include(a => a.Course)
                    .ThenInclude(c => c.Enrollments)
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.GradedBy)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                return (false, 404, "Không tìm thấy bài tập", null);
            }

            var course = assignment.Course;

            // Scope check
            if (_currentUserService.IsStudent && userId.HasValue)
            {
                if (!course.Enrollments.Any(e => e.StudentId == userId.Value))
                {
                    return (false, 403, "Bạn không có quyền truy cập bài tập này", null);
                }
            }
            else if (_currentUserService.IsTeacher && userId.HasValue)
            {
                if (course.TeacherId != userId.Value)
                {
                    return (false, 403, "Bạn không có quyền xem bài tập ngoài khóa học được phân công", null);
                }
            }
            else if (!_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền truy cập bài tập này", null);
            }

            var now = DateTime.UtcNow;
            var mySubmission = userId.HasValue
                ? assignment.Submissions.FirstOrDefault(s => s.StudentId == userId.Value)
                : null;

            var dto = new AssignmentDetailDto
            {
                Id = assignment.Id,
                CourseId = assignment.CourseId,
                CourseTitle = course.Title,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                MaxScore = assignment.MaxScore,
                IsActive = assignment.IsActive,
                CreatedAt = assignment.CreatedAt,
                IsOverdue = now > assignment.DueDate,
                MySubmission = mySubmission != null ? new SubmissionDto
                {
                    Id = mySubmission.Id,
                    AssignmentId = mySubmission.AssignmentId,
                    AssignmentTitle = assignment.Title,
                    StudentId = mySubmission.StudentId,
                    TextContent = mySubmission.TextContent,
                    FileName = mySubmission.FileName,
                    SubmittedAt = mySubmission.SubmittedAt,
                    Status = mySubmission.Status,
                    Score = mySubmission.Score,
                    Feedback = mySubmission.Feedback,
                    GradedAt = mySubmission.GradedAt,
                    GradedByName = mySubmission.GradedBy?.FullName
                } : null
            };

            return (true, 200, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, AssignmentDto Data)> CreateAssignmentAsync(int courseId, CreateAssignmentRequest request)
        {
            var userId = _currentUserService.UserId;

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null)
            {
                return (false, 404, "Không tìm thấy khóa học", null);
            }

            if (_currentUserService.IsTeacher && course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền tạo bài tập cho khóa học của giảng viên khác", null);
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này", null);
            }

            if (request.MaxScore <= 0)
            {
                return (false, 400, "Điểm tối đa phải lớn hơn 0", null);
            }

            var assignment = new Assignment
            {
                CourseId = courseId,
                Title = request.Title.Trim(),
                Description = request.Description,
                DueDate = request.DueDate,
                MaxScore = request.MaxScore,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            var dto = new AssignmentDto
            {
                Id = assignment.Id,
                CourseId = assignment.CourseId,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                MaxScore = assignment.MaxScore,
                IsActive = assignment.IsActive,
                CreatedAt = assignment.CreatedAt,
                IsOverdue = DateTime.UtcNow > assignment.DueDate,
                HasSubmitted = false,
                SubmissionStatus = "NOT_SUBMITTED",
                SubmissionCount = 0,
                GradedCount = 0
            };

            return (true, 201, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, AssignmentDto Data)> UpdateAssignmentAsync(int assignmentId, UpdateAssignmentRequest request)
        {
            var userId = _currentUserService.UserId;

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                return (false, 404, "Không tìm thấy bài tập", null);
            }

            if (_currentUserService.IsTeacher && assignment.Course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền chỉnh sửa bài tập này", null);
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này", null);
            }

            if (request.MaxScore <= 0)
            {
                return (false, 400, "Điểm tối đa phải lớn hơn 0", null);
            }

            assignment.Title = request.Title.Trim();
            assignment.Description = request.Description;
            assignment.DueDate = request.DueDate;
            assignment.MaxScore = request.MaxScore;
            assignment.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            var dto = new AssignmentDto
            {
                Id = assignment.Id,
                CourseId = assignment.CourseId,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                MaxScore = assignment.MaxScore,
                IsActive = assignment.IsActive,
                CreatedAt = assignment.CreatedAt,
                IsOverdue = DateTime.UtcNow > assignment.DueDate,
                SubmissionCount = assignment.Submissions.Count,
                GradedCount = assignment.Submissions.Count(s => s.Score.HasValue)
            };

            return (true, 200, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error)> DeleteAssignmentAsync(int assignmentId)
        {
            var userId = _currentUserService.UserId;

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                return (false, 404, "Không tìm thấy bài tập");
            }

            if (_currentUserService.IsTeacher && assignment.Course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền xóa bài tập này");
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này");
            }

            // Clean up files
            foreach (var sub in assignment.Submissions)
            {
                if (!string.IsNullOrEmpty(sub.FilePath))
                {
                    try
                    {
                        var fullPath = Path.Combine(_storageRoot, sub.FilePath);
                        if (File.Exists(fullPath)) File.Delete(fullPath);
                    }
                    catch { }
                }
            }

            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return (true, 204, null);
        }

        // ==================== SUBMISSIONS ====================

        public async Task<(bool Success, int StatusCode, string Error, SubmissionDto Data)> SubmitAssignmentAsync(int assignmentId, string textContent, IFormFile file)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue || !_currentUserService.IsStudent)
            {
                return (false, 403, "Chỉ sinh viên mới có quyền nộp bài tập", null);
            }

            if (string.IsNullOrWhiteSpace(textContent) && (file == null || file.Length == 0))
            {
                return (false, 400, "Vui lòng nhập nội dung bài nộp hoặc đính kèm tệp tin", null);
            }

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                return (false, 404, "Không tìm thấy bài tập", null);
            }

            // Scope check: Student must be enrolled in course
            var isEnrolled = assignment.Course.Enrollments.Any(e => e.StudentId == userId.Value);
            if (!isEnrolled)
            {
                return (false, 403, "Bạn không có quyền nộp bài cho khóa học chưa ghi danh", null);
            }

            if (!assignment.IsActive)
            {
                return (false, 400, "Bài tập này hiện đã bị đóng, không nhận thêm bài nộp", null);
            }

            // Server UTC timestamp and late status calculation
            var now = DateTime.UtcNow;
            var submissionStatus = now <= assignment.DueDate ? "SUBMITTED_ON_TIME" : "SUBMITTED_LATE";

            string safeOriginalName = null;
            string safeStorageFileName = null;

            if (file != null && file.Length > 0)
            {
                if (file.Length > MaxFileSizeBytes)
                {
                    return (false, 400, "Kích thước tệp tin vượt quá giới hạn 25MB", null);
                }

                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                {
                    return (false, 400, $"Định dạng tệp không được hỗ trợ. Định dạng cho phép: {string.Join(", ", AllowedExtensions)}", null);
                }

                safeOriginalName = Path.GetFileName(file.FileName);
                safeStorageFileName = $"{Guid.NewGuid():N}_{safeOriginalName}";
                var fullPath = Path.Combine(_storageRoot, safeStorageFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            // Check if existing submission (Idempotent Resubmission - latest is graded)
            var submission = await _context.Submissions
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == userId.Value);

            if (submission != null)
            {
                // If new file uploaded, delete old file
                if (!string.IsNullOrEmpty(safeStorageFileName) && !string.IsNullOrEmpty(submission.FilePath))
                {
                    try
                    {
                        var oldFullPath = Path.Combine(_storageRoot, submission.FilePath);
                        if (File.Exists(oldFullPath)) File.Delete(oldFullPath);
                    }
                    catch { }
                }

                submission.TextContent = textContent;
                if (!string.IsNullOrEmpty(safeStorageFileName))
                {
                    submission.FileName = safeOriginalName;
                    submission.FilePath = safeStorageFileName;
                }
                submission.SubmittedAt = now;
                submission.Status = submissionStatus;
                submission.Score = null; // Reset grade on resubmission
                submission.Feedback = null;
                submission.GradedAt = null;
                submission.GradedById = null;

                await _context.SaveChangesAsync();
            }
            else
            {
                submission = new Submission
                {
                    AssignmentId = assignmentId,
                    StudentId = userId.Value,
                    TextContent = textContent,
                    FileName = safeOriginalName,
                    FilePath = safeStorageFileName,
                    SubmittedAt = now,
                    Status = submissionStatus
                };

                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();
                await _context.Entry(submission).Reference(s => s.Student).LoadAsync();
            }

            var dto = new SubmissionDto
            {
                Id = submission.Id,
                AssignmentId = submission.AssignmentId,
                AssignmentTitle = assignment.Title,
                StudentId = submission.StudentId,
                StudentName = submission.Student?.FullName ?? string.Empty,
                StudentUsername = submission.Student?.Username ?? string.Empty,
                TextContent = submission.TextContent,
                FileName = submission.FileName,
                SubmittedAt = submission.SubmittedAt,
                Status = submission.Status,
                Score = submission.Score,
                Feedback = submission.Feedback,
                GradedAt = submission.GradedAt
            };

            return (true, 200, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, SubmissionDto Data)> GetMySubmissionAsync(int assignmentId)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue || !_currentUserService.IsStudent)
            {
                return (false, 403, "Chỉ sinh viên mới có bài nộp cá nhân", null);
            }

            var submission = await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                .Include(s => s.GradedBy)
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == userId.Value);

            if (submission == null)
            {
                return (false, 404, "Chưa nộp bài cho bài tập này", null);
            }

            var dto = new SubmissionDto
            {
                Id = submission.Id,
                AssignmentId = submission.AssignmentId,
                AssignmentTitle = submission.Assignment?.Title,
                StudentId = submission.StudentId,
                StudentName = submission.Student?.FullName,
                StudentUsername = submission.Student?.Username,
                TextContent = submission.TextContent,
                FileName = submission.FileName,
                SubmittedAt = submission.SubmittedAt,
                Status = submission.Status,
                Score = submission.Score,
                Feedback = submission.Feedback,
                GradedAt = submission.GradedAt,
                GradedByName = submission.GradedBy?.FullName
            };

            return (true, 200, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, List<SubmissionDto> Data)> GetAssignmentSubmissionsAsync(int assignmentId)
        {
            var userId = _currentUserService.UserId;

            var assignment = await _context.Assignments
                .AsNoTracking()
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                return (false, 404, "Không tìm thấy bài tập", null);
            }

            // Scope check: Teacher owner or Admin
            if (_currentUserService.IsTeacher && assignment.Course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền xem bài nộp của khóa học này", null);
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này", null);
            }

            var submissions = await _context.Submissions
                .AsNoTracking()
                .Where(s => s.AssignmentId == assignmentId)
                .Include(s => s.Student)
                .Include(s => s.GradedBy)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            var result = submissions.Select(s => new SubmissionDto
            {
                Id = s.Id,
                AssignmentId = s.AssignmentId,
                AssignmentTitle = assignment.Title,
                StudentId = s.StudentId,
                StudentName = s.Student?.FullName ?? string.Empty,
                StudentUsername = s.Student?.Username ?? string.Empty,
                TextContent = s.TextContent,
                FileName = s.FileName,
                SubmittedAt = s.SubmittedAt,
                Status = s.Status,
                Score = s.Score,
                Feedback = s.Feedback,
                GradedAt = s.GradedAt,
                GradedByName = s.GradedBy?.FullName
            }).ToList();

            return (true, 200, null, result);
        }

        public async Task<(bool Success, int StatusCode, string Error, SubmissionDto Data)> GradeSubmissionAsync(int submissionId, GradeSubmissionRequest request)
        {
            var userId = _currentUserService.UserId;

            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Course)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return (false, 404, "Không tìm thấy bài nộp", null);
            }

            // Scope check
            if (_currentUserService.IsTeacher && submission.Assignment.Course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền chấm bài nộp ngoài khóa học được phân công", null);
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này", null);
            }

            // Strict score validation
            if (request.Score < 0 || request.Score > submission.Assignment.MaxScore)
            {
                return (false, 400, $"Điểm số không hợp lệ. Điểm phải nằm trong thang điểm [0, {submission.Assignment.MaxScore}]", null);
            }

            submission.Score = request.Score;
            submission.Feedback = request.Feedback;
            submission.GradedAt = DateTime.UtcNow;
            submission.GradedById = userId;

            await _context.SaveChangesAsync();

            var grader = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);

            var dto = new SubmissionDto
            {
                Id = submission.Id,
                AssignmentId = submission.AssignmentId,
                AssignmentTitle = submission.Assignment.Title,
                StudentId = submission.StudentId,
                StudentName = submission.Student?.FullName,
                StudentUsername = submission.Student?.Username,
                TextContent = submission.TextContent,
                FileName = submission.FileName,
                SubmittedAt = submission.SubmittedAt,
                Status = submission.Status,
                Score = submission.Score,
                Feedback = submission.Feedback,
                GradedAt = submission.GradedAt,
                GradedByName = grader?.FullName
            };

            return (true, 200, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, Stream FileStream, string ContentType, string FileName)> DownloadSubmissionFileAsync(int submissionId)
        {
            var userId = _currentUserService.UserId;

            var submission = await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return (false, 404, "Không tìm thấy bài nộp", null, null, null);
            }

            if (string.IsNullOrEmpty(submission.FilePath))
            {
                return (false, 404, "Bài nộp này không có tệp tin đính kèm", null, null, null);
            }

            // Scope check: Student who submitted, Teacher owner, or Admin
            var isStudentOwner = _currentUserService.IsStudent && submission.StudentId == userId;
            var isTeacherOwner = _currentUserService.IsTeacher && submission.Assignment.Course.TeacherId == userId;
            var isAdmin = _currentUserService.IsAdmin;

            if (!isStudentOwner && !isTeacherOwner && !isAdmin)
            {
                return (false, 403, "Bạn không có quyền tải tệp bài nộp của học viên khác", null, null, null);
            }

            var fullPath = Path.Combine(_storageRoot, submission.FilePath);
            if (!File.Exists(fullPath))
            {
                return (false, 404, "Tệp tin bài nộp không tồn tại trên máy chủ", null, null, null);
            }

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (true, 200, null, stream, "application/octet-stream", submission.FileName);
        }
    }
}
