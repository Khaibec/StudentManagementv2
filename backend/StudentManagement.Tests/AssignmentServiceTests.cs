using System;
using System.Collections.Generic;
using System.IO;
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
    public class AssignmentServiceTests
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
                { "Storage:RootPath", Path.Combine(Path.GetTempPath(), "test_assignment_uploads_" + Guid.NewGuid()) }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private async Task SeedDataAsync(AppDbContext context)
        {
            var admin = new User { Id = 1, Username = "admin", Role = UserRole.Admin, PasswordHash = "x", FullName = "Admin" };
            var teacher1 = new User { Id = 2, Username = "teacher1", Role = UserRole.Teacher, PasswordHash = "x", FullName = "Teacher 1" };
            var teacher2 = new User { Id = 3, Username = "teacher2", Role = UserRole.Teacher, PasswordHash = "x", FullName = "Teacher 2" };
            var student1 = new User { Id = 4, Username = "student1", Role = UserRole.Student, PasswordHash = "x", FullName = "Student 1" };
            var student2 = new User { Id = 5, Username = "student2", Role = UserRole.Student, PasswordHash = "x", FullName = "Student 2" };

            context.Users.AddRange(admin, teacher1, teacher2, student1, student2);

            var course1 = new Course { Id = 1, Code = "CS101", Title = "Course 1", TeacherId = 2, Status = "Active" };
            var course2 = new Course { Id = 2, Code = "CS102", Title = "Course 2", TeacherId = 3, Status = "Active" };

            context.Courses.AddRange(course1, course2);

            // Student 1 only enrolled in Course 1
            context.Enrollments.Add(new Enrollment { Id = 1, CourseId = 1, StudentId = 4, Status = "Enrolled" });
            // Student 2 enrolled in Course 2
            context.Enrollments.Add(new Enrollment { Id = 2, CourseId = 2, StudentId = 5, Status = "Enrolled" });

            // Assignment on Course 1: Due in future (on-time test)
            var a1 = new Assignment
            {
                Id = 1,
                CourseId = 1,
                Title = "Assignment 1 (Future)",
                DueDate = DateTime.UtcNow.AddDays(3),
                MaxScore = 100m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Assignment on Course 1: Due in past (late test)
            var a2 = new Assignment
            {
                Id = 2,
                CourseId = 1,
                Title = "Assignment 2 (Past)",
                DueDate = DateTime.UtcNow.AddHours(-2),
                MaxScore = 100m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };

            // Assignment on Course 2
            var a3 = new Assignment
            {
                Id = 3,
                CourseId = 2,
                Title = "Assignment 3 (Course 2)",
                DueDate = DateTime.UtcNow.AddDays(1),
                MaxScore = 100m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Assignments.AddRange(a1, a2, a3);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetCourseAssignments_StudentOnlyAccessEnrolledCourse()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var config = CreateConfiguration();

            // Student 1 accesses Course 1 (enrolled) -> 200 OK
            var studentUser = new MockCurrentUserService { UserId = 4, Username = "student1", Role = "STUDENT" };
            var service = new AssignmentService(context, studentUser, config);
            var (success1, statusCode1, _, data1) = await service.GetCourseAssignmentsAsync(1);
            Assert.True(success1);
            Assert.Equal(200, statusCode1);
            Assert.Equal(2, data1.Count);

            // Student 1 accesses Course 2 (NOT enrolled) -> 403 Forbidden
            var (success2, statusCode2, error2, _) = await service.GetCourseAssignmentsAsync(2);
            Assert.False(success2);
            Assert.Equal(403, statusCode2);
            Assert.Contains("không có quyền", error2);
        }

        [Fact]
        public async Task CreateAssignment_TeacherCannotCreateForOtherTeacherCourse()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var config = CreateConfiguration();

            // Teacher 2 attempts to create assignment for Course 1 (owned by Teacher 1) -> 403
            var teacher2User = new MockCurrentUserService { UserId = 3, Username = "teacher2", Role = "TEACHER" };
            var service = new AssignmentService(context, teacher2User, config);

            var request = new CreateAssignmentRequest
            {
                Title = "Hacked Assignment",
                DueDate = DateTime.UtcNow.AddDays(5),
                MaxScore = 100m
            };

            var (success, statusCode, error, _) = await service.CreateAssignmentAsync(1, request);
            Assert.False(success);
            Assert.Equal(403, statusCode);
            Assert.Contains("không có quyền", error);
        }

        [Fact]
        public async Task SubmitAssignment_UnenrolledStudentIsRejected()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var config = CreateConfiguration();

            // Student 2 is NOT enrolled in Course 1 -> cannot submit to Assignment 1
            var student2User = new MockCurrentUserService { UserId = 5, Username = "student2", Role = "STUDENT" };
            var service = new AssignmentService(context, student2User, config);

            var (success, statusCode, error, _) = await service.SubmitAssignmentAsync(1, "My text answer", null);
            Assert.False(success);
            Assert.Equal(403, statusCode);
            Assert.Contains("không có quyền", error);
        }

        [Fact]
        public async Task SubmitAssignment_CalculatesOnTimeStatusWhenBeforeDueDate()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var config = CreateConfiguration();

            // Student 1 submits to Assignment 1 (DueDate in future)
            var student1User = new MockCurrentUserService { UserId = 4, Username = "student1", Role = "STUDENT" };
            var service = new AssignmentService(context, student1User, config);

            var (success, statusCode, _, data) = await service.SubmitAssignmentAsync(1, "Bài làm đúng hạn", null);
            Assert.True(success);
            Assert.Equal(200, statusCode);
            Assert.Equal("SUBMITTED_ON_TIME", data.Status);
            Assert.Null(data.Score);
        }

        [Fact]
        public async Task SubmitAssignment_CalculatesLateStatusWhenAfterDueDate()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var config = CreateConfiguration();

            // Student 1 submits to Assignment 2 (DueDate was 2 hours ago)
            var student1User = new MockCurrentUserService { UserId = 4, Username = "student1", Role = "STUDENT" };
            var service = new AssignmentService(context, student1User, config);

            var (success, statusCode, _, data) = await service.SubmitAssignmentAsync(2, "Bài nộp muộn", null);
            Assert.True(success);
            Assert.Equal(200, statusCode);
            Assert.Equal("SUBMITTED_LATE", data.Status);
        }

        [Fact]
        public async Task SubmitAssignment_ResubmissionUpdatesExistingRecordAndResetsGrade()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var config = CreateConfiguration();

            var student1User = new MockCurrentUserService { UserId = 4, Username = "student1", Role = "STUDENT" };
            var service = new AssignmentService(context, student1User, config);

            // First submission
            await service.SubmitAssignmentAsync(1, "Lần nộp 1", null);

            // Teacher grades it
            var sub = await context.Submissions.FirstAsync(s => s.AssignmentId == 1 && s.StudentId == 4);
            sub.Score = 90m;
            sub.Feedback = "Rất tốt";
            await context.SaveChangesAsync();

            // Student resubmits
            var (success, statusCode, _, data) = await service.SubmitAssignmentAsync(1, "Lần nộp 2 đã sửa", null);
            Assert.True(success);
            Assert.Equal("Lần nộp 2 đã sửa", data.TextContent);
            Assert.Null(data.Score); // Score reset for regrading
            Assert.Null(data.Feedback);

            var count = await context.Submissions.CountAsync(s => s.AssignmentId == 1 && s.StudentId == 4);
            Assert.Equal(1, count); // Exactly one record, not duplicated
        }

        [Fact]
        public async Task GradeSubmission_RejectsInvalidScoreBoundaries()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var config = CreateConfiguration();

            // Student 1 submits to Assignment 1 (MaxScore = 100)
            var student1User = new MockCurrentUserService { UserId = 4, Username = "student1", Role = "STUDENT" };
            var studentService = new AssignmentService(context, student1User, config);
            var (_, _, _, subData) = await studentService.SubmitAssignmentAsync(1, "Bài nộp để chấm", null);

            // Teacher 1 attempts to grade
            var teacher1User = new MockCurrentUserService { UserId = 2, Username = "teacher1", Role = "TEACHER" };
            var teacherService = new AssignmentService(context, teacher1User, config);

            // Score < 0 -> 400
            var (success1, statusCode1, error1, _) = await teacherService.GradeSubmissionAsync(subData.Id, new GradeSubmissionRequest
            {
                Score = -10m,
                Feedback = "Âm điểm"
            });
            Assert.False(success1);
            Assert.Equal(400, statusCode1);
            Assert.Contains("không hợp lệ", error1);

            // Score > 100 -> 400
            var (success2, statusCode2, error2, _) = await teacherService.GradeSubmissionAsync(subData.Id, new GradeSubmissionRequest
            {
                Score = 105m,
                Feedback = "Vượt trần điểm"
            });
            Assert.False(success2);
            Assert.Equal(400, statusCode2);
            Assert.Contains("không hợp lệ", error2);

            // Valid score: 95 -> 200
            var (success3, statusCode3, _, gradedData) = await teacherService.GradeSubmissionAsync(subData.Id, new GradeSubmissionRequest
            {
                Score = 95m,
                Feedback = "Làm bài rất tốt!"
            });
            Assert.True(success3);
            Assert.Equal(200, statusCode3);
            Assert.Equal(95m, gradedData.Score);
            Assert.Equal("Làm bài rất tốt!", gradedData.Feedback);
        }

        [Fact]
        public async Task GradeSubmission_TeacherCannotGradeOtherTeacherCourse()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var config = CreateConfiguration();

            // Student 1 submits to Assignment 1 (Course 1, Teacher 1)
            var student1User = new MockCurrentUserService { UserId = 4, Username = "student1", Role = "STUDENT" };
            var studentService = new AssignmentService(context, student1User, config);
            var (_, _, _, subData) = await studentService.SubmitAssignmentAsync(1, "Bài nộp", null);

            // Teacher 2 (teaches Course 2) attempts to grade Course 1 submission -> 403
            var teacher2User = new MockCurrentUserService { UserId = 3, Username = "teacher2", Role = "TEACHER" };
            var teacherService = new AssignmentService(context, teacher2User, config);

            var (success, statusCode, error, _) = await teacherService.GradeSubmissionAsync(subData.Id, new GradeSubmissionRequest
            {
                Score = 100m,
                Feedback = "Chấm trộm"
            });
            Assert.False(success);
            Assert.Equal(403, statusCode);
            Assert.Contains("không có quyền", error);
        }
    }
}
