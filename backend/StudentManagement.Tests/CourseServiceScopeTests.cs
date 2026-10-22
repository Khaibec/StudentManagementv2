using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Api.Data;
using StudentManagement.Api.DTOs;
using StudentManagement.Api.Entities;
using StudentManagement.Api.Services;
using Xunit;

namespace StudentManagement.Tests
{
    public class MockCurrentUserService : ICurrentUserService
    {
        public int? UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public bool IsAuthenticated => UserId.HasValue;
        public bool IsAdmin => Role == UserRole.Admin;
        public bool IsTeacher => Role == UserRole.Teacher;
        public bool IsStudent => Role == UserRole.Student;
    }

    public class CourseServiceScopeTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private async Task SeedDataAsync(AppDbContext context)
        {
            var teacher1 = new User { Id = 1, Username = "teacher1", Role = UserRole.Teacher, PasswordHash = "x", FullName = "Teacher 1" };
            var teacher2 = new User { Id = 2, Username = "teacher2", Role = UserRole.Teacher, PasswordHash = "x", FullName = "Teacher 2" };
            var student1 = new User { Id = 3, Username = "student1", Role = UserRole.Student, PasswordHash = "x", FullName = "Student 1" };
            var student2 = new User { Id = 4, Username = "student2", Role = UserRole.Student, PasswordHash = "x", FullName = "Student 2" };
            var admin = new User { Id = 5, Username = "admin", Role = UserRole.Admin, PasswordHash = "x", FullName = "Admin" };

            context.Users.AddRange(teacher1, teacher2, student1, student2, admin);

            var course1 = new Course { Id = 1, Code = "CS101", Title = "Course 1", TeacherId = 1, Status = "Active" };
            var course2 = new Course { Id = 2, Code = "CS102", Title = "Course 2", TeacherId = 2, Status = "Active" };
            var course3 = new Course { Id = 3, Code = "CS201", Title = "Course 3", TeacherId = 1, Status = "Active" };

            context.Courses.AddRange(course1, course2, course3);

            var enrollment1 = new Enrollment { Id = 1, CourseId = 1, StudentId = 3, Status = "Enrolled" };
            var enrollment2 = new Enrollment { Id = 2, CourseId = 3, StudentId = 3, Status = "Enrolled" };
            var enrollment3 = new Enrollment { Id = 3, CourseId = 2, StudentId = 4, Status = "Enrolled" };

            context.Enrollments.AddRange(enrollment1, enrollment2, enrollment3);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Student_OnlySeesEnrolledCourses()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var currentUser = new MockCurrentUserService
            {
                UserId = 3, // student1
                Username = "student1",
                Role = UserRole.Student
            };

            var courseService = new CourseService(context, currentUser);
            var courses = await courseService.GetCoursesAsync();

            // Student 1 is enrolled in course 1 and 3, but NOT 2
            Assert.Equal(2, courses.Count);
            Assert.Contains(courses, c => c.Id == 1);
            Assert.Contains(courses, c => c.Id == 3);
            Assert.DoesNotContain(courses, c => c.Id == 2);
        }

        [Fact]
        public async Task Student_AccessingUnenrolledCourse_ReturnsForbidden403()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var currentUser = new MockCurrentUserService
            {
                UserId = 3, // student1
                Username = "student1",
                Role = UserRole.Student
            };

            var courseService = new CourseService(context, currentUser);
            var result = await courseService.GetCourseByIdAsync(2); // student1 is NOT enrolled in course 2

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task Teacher_OnlySeesAssignedCourses()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var currentUser = new MockCurrentUserService
            {
                UserId = 2, // teacher2
                Username = "teacher2",
                Role = UserRole.Teacher
            };

            var courseService = new CourseService(context, currentUser);
            var courses = await courseService.GetCoursesAsync();

            // Teacher 2 is assigned ONLY course 2
            Assert.Single(courses);
            Assert.Equal(2, courses[0].Id);
        }

        [Fact]
        public async Task Teacher_UpdatingOtherTeacherCourse_ReturnsForbidden403()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var currentUser = new MockCurrentUserService
            {
                UserId = 2, // teacher2
                Username = "teacher2",
                Role = UserRole.Teacher
            };

            var courseService = new CourseService(context, currentUser);
            var result = await courseService.UpdateCourseAsync(1, new UpdateCourseRequest
            {
                Title = "Hacked Title",
                Status = "Active"
            });

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task Admin_CanSeeAllCourses()
        {
            using var context = CreateDbContext();
            await SeedDataAsync(context);

            var currentUser = new MockCurrentUserService
            {
                UserId = 5, // admin
                Username = "admin",
                Role = UserRole.Admin
            };

            var courseService = new CourseService(context, currentUser);
            var courses = await courseService.GetCoursesAsync();

            // Admin sees all 3 courses
            Assert.Equal(3, courses.Count);
        }
    }
}
