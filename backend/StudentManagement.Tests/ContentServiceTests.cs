using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudentManagement.Api.Data;
using StudentManagement.Api.DTOs;
using StudentManagement.Api.Entities;
using StudentManagement.Api.Services;
using Xunit;

namespace StudentManagement.Tests
{
    public class ContentServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private IConfiguration CreateConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                { "Storage:RootPath", Path.Combine(Path.GetTempPath(), "test_uploads_" + Guid.NewGuid()) }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private async Task SeedCourseContentAsync(AppDbContext context)
        {
            var teacher1 = new User { Id = 1, Username = "teacher1", Role = UserRole.Teacher, PasswordHash = "x", FullName = "Teacher 1" };
            var student1 = new User { Id = 2, Username = "student1", Role = UserRole.Student, PasswordHash = "x", FullName = "Student 1" };
            var student2 = new User { Id = 3, Username = "student2", Role = UserRole.Student, PasswordHash = "x", FullName = "Student 2" };

            context.Users.AddRange(teacher1, student1, student2);

            var course1 = new Course { Id = 1, Code = "CS101", Title = "Course 1", TeacherId = 1, Status = "Active" };
            var course2 = new Course { Id = 2, Code = "CS102", Title = "Course 2", TeacherId = 1, Status = "Active" };

            context.Courses.AddRange(course1, course2);

            // Student 1 only enrolled in Course 1
            context.Enrollments.Add(new Enrollment { Id = 1, CourseId = 1, StudentId = 2, Status = "Enrolled" });

            // Chapters
            var ch1 = new CourseChapter { Id = 1, CourseId = 1, Title = "Chapter 1 in Course 1", OrderIndex = 1 };
            var ch2 = new CourseChapter { Id = 2, CourseId = 2, Title = "Chapter 2 in Course 2", OrderIndex = 1 };

            context.CourseChapters.AddRange(ch1, ch2);

            // Lessons
            var lesson1 = new Lesson { Id = 1, ChapterId = 1, Title = "Lesson 1 in Course 1", OrderIndex = 1, IsPublished = true };
            var lesson2 = new Lesson { Id = 2, ChapterId = 2, Title = "Lesson 2 in Course 2", OrderIndex = 1, IsPublished = true };

            context.Lessons.AddRange(lesson1, lesson2);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Student_AccessingUnenrolledCourseLesson_Returns403()
        {
            using var context = CreateDbContext();
            await SeedCourseContentAsync(context);
            var config = CreateConfiguration();

            var currentStudent = new MockCurrentUserService
            {
                UserId = 2, // student1 (only enrolled in course 1)
                Username = "student1",
                Role = UserRole.Student
            };

            var contentService = new ContentService(context, currentStudent, config);
            var result = await contentService.GetLessonDetailAsync(2); // Lesson 2 is in course 2

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task Student_CompleteLesson_IsIdempotent()
        {
            using var context = CreateDbContext();
            await SeedCourseContentAsync(context);
            var config = CreateConfiguration();

            var currentStudent = new MockCurrentUserService
            {
                UserId = 2, // student1
                Username = "student1",
                Role = UserRole.Student
            };

            var contentService = new ContentService(context, currentStudent, config);

            // First complete call
            var result1 = await contentService.CompleteLessonAsync(1);
            Assert.True(result1.Success);
            Assert.True(result1.Data.IsCompleted);

            // Second complete call (should not throw, should not create duplicate row)
            var result2 = await contentService.CompleteLessonAsync(1);
            Assert.True(result2.Success);
            Assert.True(result2.Data.IsCompleted);

            // Verify count in DB is exactly 1
            var progressCount = await context.LessonProgresses.CountAsync(p => p.LessonId == 1 && p.StudentId == 2);
            Assert.Equal(1, progressCount);
        }

        [Fact]
        public async Task Student_CannotCompleteLessonInUnenrolledCourse_Returns403()
        {
            using var context = CreateDbContext();
            await SeedCourseContentAsync(context);
            var config = CreateConfiguration();

            var currentStudent = new MockCurrentUserService
            {
                UserId = 2, // student1 (not enrolled in Course 2)
                Username = "student1",
                Role = UserRole.Student
            };

            var contentService = new ContentService(context, currentStudent, config);
            var result = await contentService.CompleteLessonAsync(2); // Lesson 2 is in course 2

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task UploadResource_DisallowedExtension_ReturnsBadRequest400()
        {
            using var context = CreateDbContext();
            await SeedCourseContentAsync(context);
            var config = CreateConfiguration();

            var currentTeacher = new MockCurrentUserService
            {
                UserId = 1, // teacher1 (owner of course 1)
                Username = "teacher1",
                Role = UserRole.Teacher
            };

            var contentService = new ContentService(context, currentTeacher, config);

            // Disallowed exe file
            var fileBytes = Encoding.UTF8.GetBytes("fake exe content");
            var ms = new MemoryStream(fileBytes);
            var formFile = new FormFile(ms, 0, ms.Length, "file", "malicious.exe");

            var result = await contentService.UploadResourceAsync(1, formFile);

            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("không được hỗ trợ", result.Error);
        }

        [Fact]
        public async Task UploadResource_ValidPdf_Succeeds()
        {
            using var context = CreateDbContext();
            await SeedCourseContentAsync(context);
            var config = CreateConfiguration();

            var currentTeacher = new MockCurrentUserService
            {
                UserId = 1, // teacher1 (owner of course 1)
                Username = "teacher1",
                Role = UserRole.Teacher
            };

            var contentService = new ContentService(context, currentTeacher, config);

            var fileBytes = Encoding.UTF8.GetBytes("%PDF-1.4 test pdf content");
            var ms = new MemoryStream(fileBytes);
            var formFile = new FormFile(ms, 0, ms.Length, "file", "document.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var result = await contentService.UploadResourceAsync(1, formFile);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("document.pdf", result.Data.FileName);
        }
    }
}
