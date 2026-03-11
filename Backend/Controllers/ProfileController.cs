using System.Security.Claims;

using Backend.DTOs;
using Backend.DTOs.Profile;
using Backend.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/profile")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _service;

        public ProfileController(IProfileService service)
        {
            _service = service;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // ===============================
        // GET: api/profile
        // xem thông tin profile
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            int userId = GetUserId();

            var profile = await _service.GetProfileAsync(userId);

            if (profile == null)
                return NotFound("User not found");

            return Ok(profile);
        }

        // ===============================
        // PUT: api/profile
        // cập nhật profile
        // ===============================
        [HttpPut]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDTO dto)
        {
            int userId = GetUserId();

            var result = await _service.UpdateProfileAsync(userId, dto);

            if (!result)
                return BadRequest("Update failed");

            return Ok(new
            {
                message = "Profile updated successfully"
            });
        }

        // ===============================
        // PUT: api/profile/change-password
        // đổi mật khẩu
        // ===============================
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO dto)
        {
            int userId = GetUserId();

            var profile = await _service.GetProfileAsync(userId);

            if (profile == null)
                return NotFound("User not found");

            // nếu login bằng Google thì không cho đổi password
            var user = await _service.GetProfileAsync(userId);

            if (user == null)
                return NotFound();

            // logic kiểm tra google login
            // giả sử PasswordHash null = Google account
            if (await IsGoogleAccount(userId))
            {
                return BadRequest("Google account cannot change password");
            }

            var result = await _service.ChangePasswordAsync(userId, dto);

            if (!result)
                return BadRequest("Password change failed");

            return Ok(new
            {
                message = "Password changed successfully"
            });
        }

        // helper check google login
        private async Task<bool> IsGoogleAccount(int userId)
        {
            var profile = await _service.GetProfileAsync(userId);

            // nếu hệ thống bạn dùng PasswordHash null cho Google
            return profile == null;
        }
    }
}