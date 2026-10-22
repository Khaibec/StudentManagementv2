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
    public interface IContentService
    {
        // Chapters
        Task<(bool Success, int StatusCode, string Error, List<ChapterDto> Data)> GetCourseChaptersAsync(int courseId);
        Task<(bool Success, int StatusCode, string Error, ChapterDto Data)> CreateChapterAsync(int courseId, CreateChapterRequest request);
        Task<(bool Success, int StatusCode, string Error, ChapterDto Data)> UpdateChapterAsync(int chapterId, UpdateChapterRequest request);
        Task<(bool Success, int StatusCode, string Error)> DeleteChapterAsync(int chapterId);

        // Lessons
        Task<(bool Success, int StatusCode, string Error, LessonDetailDto Data)> GetLessonDetailAsync(int lessonId);
        Task<(bool Success, int StatusCode, string Error, LessonDetailDto Data)> CreateLessonAsync(int chapterId, CreateLessonRequest request);
        Task<(bool Success, int StatusCode, string Error, LessonDetailDto Data)> UpdateLessonAsync(int lessonId, UpdateLessonRequest request);
        Task<(bool Success, int StatusCode, string Error)> DeleteLessonAsync(int lessonId);

        // Progress
        Task<(bool Success, int StatusCode, string Error, LessonProgressDto Data)> CompleteLessonAsync(int lessonId);

        // Resources
        Task<(bool Success, int StatusCode, string Error, ResourceDto Data)> UploadResourceAsync(int lessonId, IFormFile file);
        Task<(bool Success, int StatusCode, string Error, Stream FileStream, string ContentType, string FileName)> DownloadResourceAsync(int resourceId);
        Task<(bool Success, int StatusCode, string Error)> DeleteResourceAsync(int resourceId);
    }

    public class ContentService : IContentService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly string _storageRoot;

        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx", ".doc", ".pptx", ".ppt", ".xlsx", ".xls", ".txt", ".zip", ".png", ".jpg", ".jpeg"
        };

        private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

        public ContentService(AppDbContext context, ICurrentUserService currentUserService, IConfiguration configuration)
        {
            _context = context;
            _currentUserService = currentUserService;
            var relativeStorage = configuration.GetValue<string>("Storage:RootPath") ?? "uploads";
            _storageRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeStorage));
            if (!Directory.Exists(_storageRoot))
            {
                Directory.CreateDirectory(_storageRoot);
            }
        }

        // ==================== CHAPTERS ====================

        public async Task<(bool Success, int StatusCode, string Error, List<ChapterDto> Data)> GetCourseChaptersAsync(int courseId)
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
                    return (false, 403, "Bạn không có quyền truy cập khóa học này", null);
                }
            }
            else if (_currentUserService.IsTeacher && userId.HasValue)
            {
                if (course.TeacherId != userId.Value)
                {
                    return (false, 403, "Bạn không có quyền truy cập khóa học này", null);
                }
            }
            else if (!_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền truy cập", null);
            }

            var chapters = await _context.CourseChapters
                .AsNoTracking()
                .Where(ch => ch.CourseId == courseId)
                .OrderBy(ch => ch.OrderIndex)
                .ThenBy(ch => ch.Id)
                .Include(ch => ch.Lessons)
                    .ThenInclude(l => l.Progresses)
                .Include(ch => ch.Lessons)
                    .ThenInclude(l => l.Resources)
                .ToListAsync();

            var result = chapters.Select(ch => new ChapterDto
            {
                Id = ch.Id,
                CourseId = ch.CourseId,
                Title = ch.Title,
                Description = ch.Description,
                OrderIndex = ch.OrderIndex,
                CreatedAt = ch.CreatedAt,
                Lessons = ch.Lessons
                    .Where(l => !_currentUserService.IsStudent || l.IsPublished)
                    .OrderBy(l => l.OrderIndex)
                    .ThenBy(l => l.Id)
                    .Select(l =>
                    {
                        var studentProgress = userId.HasValue
                            ? l.Progresses.FirstOrDefault(p => p.StudentId == userId.Value)
                            : null;

                        return new LessonSummaryDto
                        {
                            Id = l.Id,
                            ChapterId = l.ChapterId,
                            Title = l.Title,
                            OrderIndex = l.OrderIndex,
                            IsPublished = l.IsPublished,
                            IsCompleted = studentProgress?.IsCompleted ?? false,
                            CompletedAt = studentProgress?.CompletedAt,
                            ResourceCount = l.Resources.Count
                        };
                    }).ToList()
            }).ToList();

            return (true, 200, null, result);
        }

        public async Task<(bool Success, int StatusCode, string Error, ChapterDto Data)> CreateChapterAsync(int courseId, CreateChapterRequest request)
        {
            var userId = _currentUserService.UserId;

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null)
            {
                return (false, 404, "Không tìm thấy khóa học", null);
            }

            if (_currentUserService.IsTeacher && course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền quản lý chương của khóa học này", null);
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này", null);
            }

            var chapter = new CourseChapter
            {
                CourseId = courseId,
                Title = request.Title.Trim(),
                Description = request.Description,
                OrderIndex = request.OrderIndex,
                CreatedAt = DateTime.UtcNow
            };

            _context.CourseChapters.Add(chapter);
            await _context.SaveChangesAsync();

            var dto = new ChapterDto
            {
                Id = chapter.Id,
                CourseId = chapter.CourseId,
                Title = chapter.Title,
                Description = chapter.Description,
                OrderIndex = chapter.OrderIndex,
                CreatedAt = chapter.CreatedAt,
                Lessons = new List<LessonSummaryDto>()
            };

            return (true, 201, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, ChapterDto Data)> UpdateChapterAsync(int chapterId, UpdateChapterRequest request)
        {
            var userId = _currentUserService.UserId;

            var chapter = await _context.CourseChapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == chapterId);

            if (chapter == null)
            {
                return (false, 404, "Không tìm thấy chương học", null);
            }

            if (_currentUserService.IsTeacher && chapter.Course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền chỉnh sửa chương của khóa học này", null);
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này", null);
            }

            chapter.Title = request.Title.Trim();
            chapter.Description = request.Description;
            chapter.OrderIndex = request.OrderIndex;

            await _context.SaveChangesAsync();

            var dto = new ChapterDto
            {
                Id = chapter.Id,
                CourseId = chapter.CourseId,
                Title = chapter.Title,
                Description = chapter.Description,
                OrderIndex = chapter.OrderIndex,
                CreatedAt = chapter.CreatedAt
            };

            return (true, 200, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error)> DeleteChapterAsync(int chapterId)
        {
            var userId = _currentUserService.UserId;

            var chapter = await _context.CourseChapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == chapterId);

            if (chapter == null)
            {
                return (false, 404, "Không tìm thấy chương học");
            }

            if (_currentUserService.IsTeacher && chapter.Course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền xóa chương của khóa học này");
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này");
            }

            _context.CourseChapters.Remove(chapter);
            await _context.SaveChangesAsync();

            return (true, 204, null);
        }

        // ==================== LESSONS ====================

        public async Task<(bool Success, int StatusCode, string Error, LessonDetailDto Data)> GetLessonDetailAsync(int lessonId)
        {
            var userId = _currentUserService.UserId;

            var lesson = await _context.Lessons
                .AsNoTracking()
                .Include(l => l.Chapter)
                    .ThenInclude(ch => ch.Course)
                        .ThenInclude(c => c.Enrollments)
                .Include(l => l.Resources)
                .Include(l => l.Progresses)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return (false, 404, "Không tìm thấy bài học", null);
            }

            var course = lesson.Chapter?.Course;
            if (course == null)
            {
                return (false, 404, "Không tìm thấy thông tin khóa học liên kết", null);
            }

            // Authorization / Scope check
            if (_currentUserService.IsStudent && userId.HasValue)
            {
                var isEnrolled = course.Enrollments.Any(e => e.StudentId == userId.Value);
                if (!isEnrolled)
                {
                    return (false, 403, "Bạn không có quyền truy cập bài học này", null);
                }

                if (!lesson.IsPublished)
                {
                    return (false, 403, "Bài học này hiện chưa được công khai", null);
                }
            }
            else if (_currentUserService.IsTeacher && userId.HasValue)
            {
                if (course.TeacherId != userId.Value)
                {
                    return (false, 403, "Bạn không có quyền xem bài học ngoài khóa học được phân công", null);
                }
            }
            else if (!_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền truy cập bài học này", null);
            }

            var studentProgress = userId.HasValue
                ? lesson.Progresses.FirstOrDefault(p => p.StudentId == userId.Value)
                : null;

            var dto = new LessonDetailDto
            {
                Id = lesson.Id,
                ChapterId = lesson.ChapterId,
                ChapterTitle = lesson.Chapter?.Title ?? string.Empty,
                CourseId = course.Id,
                CourseTitle = course.Title,
                Title = lesson.Title,
                Description = lesson.Description,
                OrderIndex = lesson.OrderIndex,
                VideoUrl = lesson.VideoUrl,
                IsPublished = lesson.IsPublished,
                CreatedAt = lesson.CreatedAt,
                IsCompleted = studentProgress?.IsCompleted ?? false,
                CompletedAt = studentProgress?.CompletedAt,
                Resources = lesson.Resources.Select(r => new ResourceDto
                {
                    Id = r.Id,
                    LessonId = r.LessonId,
                    FileName = r.FileName,
                    ContentType = r.ContentType,
                    FileSize = r.FileSize,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return (true, 200, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, LessonDetailDto Data)> CreateLessonAsync(int chapterId, CreateLessonRequest request)
        {
            var userId = _currentUserService.UserId;

            var chapter = await _context.CourseChapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == chapterId);

            if (chapter == null)
            {
                return (false, 404, "Không tìm thấy chương học", null);
            }

            if (_currentUserService.IsTeacher && chapter.Course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền tạo bài học trong khóa này", null);
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này", null);
            }

            var lesson = new Lesson
            {
                ChapterId = chapterId,
                Title = request.Title.Trim(),
                Description = request.Description,
                OrderIndex = request.OrderIndex,
                VideoUrl = request.VideoUrl,
                IsPublished = request.IsPublished,
                CreatedAt = DateTime.UtcNow
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            var dto = new LessonDetailDto
            {
                Id = lesson.Id,
                ChapterId = lesson.ChapterId,
                ChapterTitle = chapter.Title,
                CourseId = chapter.CourseId,
                CourseTitle = chapter.Course.Title,
                Title = lesson.Title,
                Description = lesson.Description,
                OrderIndex = lesson.OrderIndex,
                VideoUrl = lesson.VideoUrl,
                IsPublished = lesson.IsPublished,
                CreatedAt = lesson.CreatedAt,
                IsCompleted = false,
                Resources = new List<ResourceDto>()
            };

            return (true, 201, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, LessonDetailDto Data)> UpdateLessonAsync(int lessonId, UpdateLessonRequest request)
        {
            var userId = _currentUserService.UserId;

            var lesson = await _context.Lessons
                .Include(l => l.Chapter)
                    .ThenInclude(ch => ch.Course)
                .Include(l => l.Resources)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return (false, 404, "Không tìm thấy bài học", null);
            }

            if (_currentUserService.IsTeacher && lesson.Chapter.Course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền chỉnh sửa bài học này", null);
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này", null);
            }

            lesson.Title = request.Title.Trim();
            lesson.Description = request.Description;
            lesson.OrderIndex = request.OrderIndex;
            lesson.VideoUrl = request.VideoUrl;
            lesson.IsPublished = request.IsPublished;

            await _context.SaveChangesAsync();

            var dto = new LessonDetailDto
            {
                Id = lesson.Id,
                ChapterId = lesson.ChapterId,
                ChapterTitle = lesson.Chapter.Title,
                CourseId = lesson.Chapter.CourseId,
                CourseTitle = lesson.Chapter.Course.Title,
                Title = lesson.Title,
                Description = lesson.Description,
                OrderIndex = lesson.OrderIndex,
                VideoUrl = lesson.VideoUrl,
                IsPublished = lesson.IsPublished,
                CreatedAt = lesson.CreatedAt,
                Resources = lesson.Resources.Select(r => new ResourceDto
                {
                    Id = r.Id,
                    LessonId = r.LessonId,
                    FileName = r.FileName,
                    ContentType = r.ContentType,
                    FileSize = r.FileSize,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return (true, 200, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error)> DeleteLessonAsync(int lessonId)
        {
            var userId = _currentUserService.UserId;

            var lesson = await _context.Lessons
                .Include(l => l.Chapter)
                    .ThenInclude(ch => ch.Course)
                .Include(l => l.Resources)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return (false, 404, "Không tìm thấy bài học");
            }

            if (_currentUserService.IsTeacher && lesson.Chapter.Course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền xóa bài học này");
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này");
            }

            // Remove associated files from disk
            foreach (var res in lesson.Resources)
            {
                try
                {
                    var fullPath = Path.Combine(_storageRoot, res.FilePath);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
                catch { }
            }

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            return (true, 204, null);
        }

        // ==================== PROGRESS (IDEMPOTENT) ====================

        public async Task<(bool Success, int StatusCode, string Error, LessonProgressDto Data)> CompleteLessonAsync(int lessonId)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue || !_currentUserService.IsStudent)
            {
                return (false, 403, "Chỉ sinh viên mới có quyền đánh dấu hoàn thành bài học", null);
            }

            var lesson = await _context.Lessons
                .AsNoTracking()
                .Include(l => l.Chapter)
                    .ThenInclude(ch => ch.Course)
                        .ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return (false, 404, "Không tìm thấy bài học", null);
            }

            // Scope check: Student must be enrolled in this course
            var isEnrolled = lesson.Chapter?.Course?.Enrollments.Any(e => e.StudentId == userId.Value) ?? false;
            if (!isEnrolled)
            {
                return (false, 403, "Bạn không thể đánh dấu bài học ngoài khóa học đã ghi danh", null);
            }

            // Check if progress already exists (Idempotent handling)
            var progress = await _context.LessonProgresses
                .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.StudentId == userId.Value);

            if (progress != null)
            {
                // Idempotent: already exists, ensure it is set to completed
                progress.IsCompleted = true;
                progress.CompletedAt ??= DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else
            {
                progress = new LessonProgress
                {
                    LessonId = lessonId,
                    StudentId = userId.Value,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                };
                _context.LessonProgresses.Add(progress);
                await _context.SaveChangesAsync();
            }

            var dto = new LessonProgressDto
            {
                LessonId = progress.LessonId,
                StudentId = progress.StudentId,
                IsCompleted = progress.IsCompleted,
                CompletedAt = progress.CompletedAt
            };

            return (true, 200, null, dto);
        }

        // ==================== RESOURCES ====================

        public async Task<(bool Success, int StatusCode, string Error, ResourceDto Data)> UploadResourceAsync(int lessonId, IFormFile file)
        {
            var userId = _currentUserService.UserId;

            if (file == null || file.Length == 0)
            {
                return (false, 400, "Vui lòng chọn tệp tin cần tải lên", null);
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return (false, 400, "Kích thước tệp tin vượt quá giới hạn cho phép (tối đa 25MB)", null);
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                return (false, 400, $"Định dạng tệp không được hỗ trợ. Định dạng cho phép: {string.Join(", ", AllowedExtensions)}", null);
            }

            var lesson = await _context.Lessons
                .Include(l => l.Chapter)
                    .ThenInclude(ch => ch.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return (false, 404, "Không tìm thấy bài học", null);
            }

            if (_currentUserService.IsTeacher && lesson.Chapter.Course.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền tải lên tài liệu cho khóa học này", null);
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này", null);
            }

            // Secure filename generation (prevents directory traversal attacks)
            var safeOriginalName = Path.GetFileName(file.FileName);
            var safeStorageFileName = $"{Guid.NewGuid():N}_{safeOriginalName}";
            var fullPath = Path.Combine(_storageRoot, safeStorageFileName);

            // Double check traversal safety
            if (!fullPath.StartsWith(_storageRoot, StringComparison.OrdinalIgnoreCase))
            {
                return (false, 400, "Tên tệp không hợp lệ", null);
            }

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var resource = new LessonResource
            {
                LessonId = lessonId,
                FileName = safeOriginalName,
                FilePath = safeStorageFileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            _context.LessonResources.Add(resource);
            await _context.SaveChangesAsync();

            var dto = new ResourceDto
            {
                Id = resource.Id,
                LessonId = resource.LessonId,
                FileName = resource.FileName,
                ContentType = resource.ContentType,
                FileSize = resource.FileSize,
                CreatedAt = resource.CreatedAt
            };

            return (true, 201, null, dto);
        }

        public async Task<(bool Success, int StatusCode, string Error, Stream FileStream, string ContentType, string FileName)> DownloadResourceAsync(int resourceId)
        {
            var userId = _currentUserService.UserId;

            var resource = await _context.LessonResources
                .AsNoTracking()
                .Include(r => r.Lesson)
                    .ThenInclude(l => l.Chapter)
                        .ThenInclude(ch => ch.Course)
                            .ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(r => r.Id == resourceId);

            if (resource == null)
            {
                return (false, 404, "Không tìm thấy tài liệu", null, null, null);
            }

            var course = resource.Lesson?.Chapter?.Course;
            if (course == null)
            {
                return (false, 404, "Không tìm thấy khóa học chứa tài liệu", null, null, null);
            }

            // Authorization / Scope check
            if (_currentUserService.IsStudent && userId.HasValue)
            {
                var isEnrolled = course.Enrollments.Any(e => e.StudentId == userId.Value);
                if (!isEnrolled)
                {
                    return (false, 403, "Bạn không có quyền tải tài liệu của khóa học này", null, null, null);
                }
            }
            else if (_currentUserService.IsTeacher && userId.HasValue)
            {
                if (course.TeacherId != userId.Value)
                {
                    return (false, 403, "Bạn không có quyền truy cập tài liệu ngoài khóa học được phân công", null, null, null);
                }
            }
            else if (!_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền tải tài liệu", null, null, null);
            }

            var fullPath = Path.Combine(_storageRoot, resource.FilePath);
            if (!File.Exists(fullPath))
            {
                return (false, 404, "Tệp tin vật lý không tồn tại trên máy chủ", null, null, null);
            }

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = !string.IsNullOrEmpty(resource.ContentType) ? resource.ContentType : "application/octet-stream";

            return (true, 200, null, stream, contentType, resource.FileName);
        }

        public async Task<(bool Success, int StatusCode, string Error)> DeleteResourceAsync(int resourceId)
        {
            var userId = _currentUserService.UserId;

            var resource = await _context.LessonResources
                .Include(r => r.Lesson)
                    .ThenInclude(l => l.Chapter)
                        .ThenInclude(ch => ch.Course)
                .FirstOrDefaultAsync(r => r.Id == resourceId);

            if (resource == null)
            {
                return (false, 404, "Không tìm thấy tài liệu");
            }

            var course = resource.Lesson?.Chapter?.Course;
            if (_currentUserService.IsTeacher && course?.TeacherId != userId)
            {
                return (false, 403, "Bạn không có quyền xóa tài liệu của khóa học này");
            }

            if (!_currentUserService.IsTeacher && !_currentUserService.IsAdmin)
            {
                return (false, 403, "Bạn không có quyền thực hiện thao tác này");
            }

            try
            {
                var fullPath = Path.Combine(_storageRoot, resource.FilePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch { }

            _context.LessonResources.Remove(resource);
            await _context.SaveChangesAsync();

            return (true, 204, null);
        }
    }
}
