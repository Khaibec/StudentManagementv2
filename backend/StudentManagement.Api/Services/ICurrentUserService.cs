using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StudentManagement.Api.Entities;

namespace StudentManagement.Api.Services
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string Username { get; }
        string Role { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        bool IsTeacher { get; }
        bool IsStudent { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        public int? UserId
        {
            get
            {
                var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User?.FindFirst("id")?.Value
                           ?? User?.FindFirst("sub")?.Value;

                return int.TryParse(idClaim, out var id) ? id : (int?)null;
            }
        }

        public string Username => User?.Identity?.Name ?? User?.FindFirst("username")?.Value;

        public string Role => User?.FindFirst(ClaimTypes.Role)?.Value ?? User?.FindFirst("role")?.Value;

        public bool IsAdmin => string.Equals(Role, UserRole.Admin, System.StringComparison.OrdinalIgnoreCase);

        public bool IsTeacher => string.Equals(Role, UserRole.Teacher, System.StringComparison.OrdinalIgnoreCase);

        public bool IsStudent => string.Equals(Role, UserRole.Student, System.StringComparison.OrdinalIgnoreCase);
    }
}
