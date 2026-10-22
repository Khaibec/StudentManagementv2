using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudentManagement.Api.Data;
using StudentManagement.Api.DTOs;
using StudentManagement.Api.Entities;
using StudentManagement.Api.Services;
using Xunit;

namespace StudentManagement.Tests
{
    public class AuthServiceTests
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
                { "Jwt:Key", "TestSecretKeyAtLeast32CharactersLong12345!" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" },
                { "Jwt:ExpiresInMinutes", "60" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsTokenAndUserInfo()
        {
            using var context = CreateDbContext();
            var passwordHasher = new PasswordHasher();
            var config = CreateConfiguration();

            var user = new User
            {
                Username = "teststudent",
                Email = "student@test.local",
                FullName = "Nguyễn Test",
                Role = UserRole.Student,
                PasswordHash = passwordHasher.HashPassword("Password@123"),
                IsActive = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var authService = new AuthService(context, passwordHasher, config);

            var result = await authService.LoginAsync(new LoginRequest
            {
                Username = "teststudent",
                Password = "Password@123"
            });

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data.Token);
            Assert.Equal("teststudent", result.Data.Username);
            Assert.Equal(UserRole.Student, result.Data.Role);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsGenericErrorMessage()
        {
            using var context = CreateDbContext();
            var passwordHasher = new PasswordHasher();
            var config = CreateConfiguration();

            var user = new User
            {
                Username = "teststudent",
                Email = "student@test.local",
                FullName = "Nguyễn Test",
                Role = UserRole.Student,
                PasswordHash = passwordHasher.HashPassword("Password@123"),
                IsActive = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var authService = new AuthService(context, passwordHasher, config);

            var result = await authService.LoginAsync(new LoginRequest
            {
                Username = "teststudent",
                Password = "WrongPassword"
            });

            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác", result.Error);
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ReturnsSameGenericErrorMessage()
        {
            using var context = CreateDbContext();
            var passwordHasher = new PasswordHasher();
            var config = CreateConfiguration();

            var authService = new AuthService(context, passwordHasher, config);

            var result = await authService.LoginAsync(new LoginRequest
            {
                Username = "nonexistent",
                Password = "AnyPassword"
            });

            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Tên đăng nhập hoặc mật khẩu không chính xác", result.Error);
        }
    }
}
