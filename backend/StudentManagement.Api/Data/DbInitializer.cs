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
            await context.Database.EnsureCreatedAsync();

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

            // Seed Chapters and Lessons if none exist
            if (!await context.CourseChapters.AnyAsync())
            {
                var course1 = await context.Courses.FirstOrDefaultAsync(c => c.Code == "CS101");
                if (course1 != null)
                {
                    var ch1 = new CourseChapter
                    {
                        CourseId = course1.Id,
                        Title = "Chương 1: Tổng quan về ngôn ngữ C# và .NET",
                        Description = "Giới thiệu nền tảng .NET 5, CLR, CTS và cú pháp C# căn bản.",
                        OrderIndex = 1,
                        CreatedAt = DateTime.UtcNow
                    };

                    var ch2 = new CourseChapter
                    {
                        CourseId = course1.Id,
                        Title = "Chương 2: Lập trình hướng đối tượng (OOP) trong C#",
                        Description = "Lớp, đối tượng, tính đóng gói, kế thừa và đa hình.",
                        OrderIndex = 2,
                        CreatedAt = DateTime.UtcNow
                    };

                    await context.CourseChapters.AddRangeAsync(ch1, ch2);
                    await context.SaveChangesAsync();

                    var lesson1 = new Lesson
                    {
                        ChapterId = ch1.Id,
                        Title = "Bài 1.1: Giới thiệu kiến trúc .NET 5 và Cài đặt môi trường",
                        Description = "Cài đặt .NET 5 SDK và Visual Studio Code trên máy tính.",
                        OrderIndex = 1,
                        VideoUrl = "https://www.youtube.com/watch?v=0kFh2oNnU7k",
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var lesson2 = new Lesson
                    {
                        ChapterId = ch1.Id,
                        Title = "Bài 1.2: Các kiểu dữ liệu cơ bản, Biến và Cấu trúc điều khiển",
                        Description = "Tìm hiểu int, string, bool, if-else, switch-case và vòng lặp for/while.",
                        OrderIndex = 2,
                        VideoUrl = "https://www.youtube.com/watch?v=0kFh2oNnU7k",
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var lesson3 = new Lesson
                    {
                        ChapterId = ch2.Id,
                        Title = "Bài 2.1: Class, Struct và Namespace",
                        Description = "Khai báo class, khởi tạo đối tượng với từ khóa new và đóng gói namespace.",
                        OrderIndex = 1,
                        VideoUrl = "https://www.youtube.com/watch?v=0kFh2oNnU7k",
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await context.Lessons.AddRangeAsync(lesson1, lesson2, lesson3);
                    await context.SaveChangesAsync();
                }
            }

            // Seed Assignments if none exist
            if (!await context.Assignments.AnyAsync())
            {
                var course1 = await context.Courses.FirstOrDefaultAsync(c => c.Code == "CS101");
                if (course1 != null)
                {
                    var a1 = new Assignment
                    {
                        CourseId = course1.Id,
                        Title = "Bài tập 1: Giải phương trình bậc 2 và Quản lý mảng trong C#",
                        Description = "Viết chương trình C# giải phương trình bậc 2 và thực hiện sắp xếp danh sách học sinh. Nộp mã nguồn hoặc tệp đính kèm.",
                        DueDate = DateTime.UtcNow.AddDays(7),
                        MaxScore = 100m,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var a2 = new Assignment
                    {
                        CourseId = course1.Id,
                        Title = "Bài tập 2: Thiết kế mô hình Lớp kế thừa và Đa hình (OOP)",
                        Description = "Xây dựng hệ thống class hướng đối tượng áp dụng các nguyên lý OOP cơ bản. (Bài tập quá hạn để kiểm tra nộp muộn).",
                        DueDate = DateTime.UtcNow.AddDays(-2),
                        MaxScore = 100m,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    };

                    await context.Assignments.AddRangeAsync(a1, a2);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
