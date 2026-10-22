using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StudentManagement.Api.Data;
using StudentManagement.Api.DTOs;
using StudentManagement.Api.Entities;

namespace StudentManagement.Api.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Error, LoginResponse Data)> LoginAsync(LoginRequest request);
        Task<(bool Success, string Error, CurrentUserResponse Data)> GetCurrentUserAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IPasswordHasher passwordHasher, IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        public async Task<(bool Success, string Error, LoginResponse Data)> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return (false, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu", null);
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            // Generic error message to prevent user enumeration
            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return (false, "Tên đăng nhập hoặc mật khẩu không chính xác", null);
            }

            if (!user.IsActive)
            {
                return (false, "Tài khoản hiện đang bị khóa", null);
            }

            var jwtSettings = _configuration.GetSection("Jwt");
            var keyString = jwtSettings["Key"] ?? "StudentManagementv2_DefaultSecretKey_AtLeast32Chars!";
            var issuer = jwtSettings["Issuer"] ?? "StudentManagementApi";
            var audience = jwtSettings["Audience"] ?? "StudentManagementClient";
            var minutes = int.TryParse(jwtSettings["ExpiresInMinutes"], out var m) ? m : 120;

            var expiresAt = DateTime.UtcNow.AddMinutes(minutes);
            var tokenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var credentials = new SigningCredentials(tokenKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("id", user.Id.ToString()),
                new Claim("role", user.Role),
                new Claim("fullName", user.FullName ?? user.Username)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var response = new LoginResponse
            {
                Token = tokenString,
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                ExpiresAt = expiresAt
            };

            return (true, null, response);
        }

        public async Task<(bool Success, string Error, CurrentUserResponse Data)> GetCurrentUserAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return (false, "Không tìm thấy người dùng", null);
            }

            return (true, null, new CurrentUserResponse
            {
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            });
        }
    }
}
