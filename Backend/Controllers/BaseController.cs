using System.Security.Claims;
using Backend.Common;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected int GetCurrentUserId()
        {
            var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdString, out var userId) ? userId : 0;
        }

        protected string? GetCurrentUserRole()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        }

        protected string? GetCurrentUserEmail()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }

        protected bool IsTeacher()
        {
            return GetCurrentUserRole() == RoleIds.Teacher;
        }

        protected bool IsStudent()
        {
            return GetCurrentUserRole() == RoleIds.Student;
        }

        protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

        protected IActionResult SuccessResponse<T>(T data, string message = "Thành công")
        {
            return Ok(new { success = true, message, data });
        }

        protected IActionResult ErrorResponse(string message, int statusCode = 400, object? details = null)
        {
            return StatusCode(statusCode, new { success = false, message, details });
        }

        protected IActionResult Forbidden(string message = "Bạn không có quyền truy cập tài nguyên này.")
        {
            return StatusCode(403, new { success = false, message });
        }
    }
}
