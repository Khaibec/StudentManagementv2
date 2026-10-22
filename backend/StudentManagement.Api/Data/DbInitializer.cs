using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Api.Entities;
using StudentManagement.Api.Services;

namespace StudentManagement.Api.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(AppDbContext context, IPasswordHasher passwordHasher)
        {
            // Create database tables if not exist (idempotent, safe)
            await context.Database.EnsureCreatedAsync();

            // Seed only if no users exist (do NOT overwrite existing data)
            if (!await context.Users.AnyAsync())
            {
                var admin = new User
                {
                    Username = "admin",
                    Email = "admin@lms.local",
                    FullName = "Quản Trị Viên Hệ Thống",
                    Role = UserRole.Admin,
                    PasswordHash = passwordHasher.HashPassword("Admin@123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var teacher1 = new User
                {
                    Username = "teacher1",
                    Email = "teacher1@lms.local",
                    FullName = "Thầy Nguyễn Văn A",
                    Role = UserRole.Teacher,
                    PasswordHash = passwordHasher.HashPassword("Teacher@123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var teacher2 = new User
                {
                    Username = "teacher2",
                    Email = "teacher2@lms.local",
                    FullName = "Cô Trần Thị B",
                    Role = UserRole.Teacher,
                    PasswordHash = passwordHasher.HashPassword("Teacher@123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var student1 = new User
                {
                    Username = "student1",
                    Email = "student1@lms.local",
                    FullName = "Học sinh Lê Văn C",
                    Role = UserRole.Student,
                    PasswordHash = passwordHasher.HashPassword("Student@123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var student2 = new User
                {
                    Username = "student2",
                    Email = "student2@lms.local",
                    FullName = "Học sinh Phạm Thị D",
                    Role = UserRole.Student,
                    PasswordHash = passwordHasher.HashPassword("Student@123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddRangeAsync(admin, teacher1, teacher2, student1, student2);
                await context.SaveChangesAsync();

                var course1 = new Course
                {
                    Code = "CS101",
                    Title = "Lập trình C# và .NET Căn Bản",
                    Description = "Khóa học nền tảng về ngôn ngữ C# và nền tảng .NET 5 cho sinh viên công nghệ thông tin.",
                    TeacherId = teacher1.Id,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                var course2 = new Course
                {
                    Code = "CS102",
                    Title = "Cơ sở dữ liệu và SQL Server",
                    Description = "Khóa học chuyên sâu về thiết kế CSDL quan hệ, chỉ mục và tối ưu truy vấn SQL.",
                    TeacherId = teacher2.Id,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                var course3 = new Course
                {
                    Code = "CS201",
                    Title = "Kiến trúc Web API và Bảo mật Hệ thống",
                    Description = "Khóa học nâng cao về RESTful API, xác thực JWT, phân quyền và triển khai LMS.",
                    TeacherId = teacher1.Id,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                await context.Courses.AddRangeAsync(course1, course2, course3);
                await context.SaveChangesAsync();

                var enrollments = new List<Enrollment>
                {
                    new Enrollment { CourseId = course1.Id, StudentId = student1.Id, Status = "Enrolled", EnrolledAt = DateTime.UtcNow },
                    new Enrollment { CourseId = course3.Id, StudentId = student1.Id, Status = "Enrolled", EnrolledAt = DateTime.UtcNow },
                    new Enrollment { CourseId = course2.Id, StudentId = student2.Id, Status = "Enrolled", EnrolledAt = DateTime.UtcNow },
                    new Enrollment { CourseId = course3.Id, StudentId = student2.Id, Status = "Enrolled", EnrolledAt = DateTime.UtcNow }
                };

                await context.Enrollments.AddRangeAsync(enrollments);
                await context.SaveChangesAsync();
            }
        }
    }
}
