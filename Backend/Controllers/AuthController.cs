using System.Security.Claims;
using Backend.DTOs;
using Backend.DTOs.Auth;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Constants;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetMe()
        {
            var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId) || userId <= 0)
                return Unauthorized(new { message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });

            return Ok(new { userId, email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value, role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ErrorMessages.LoginProcessingError, details = ex.Message });
            }
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var response = await _authService.GoogleLoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ErrorMessages.GoogleLoginProcessingError, details = ex.Message });
            }
        }

        [HttpPost("google-register")]
        public async Task<IActionResult> GoogleRegister([FromBody] GoogleRegisterRequest request)
        {
            try
            {
                var response = await _authService.GoogleRegisterAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ErrorMessages.GoogleRegistrationProcessingError, details = ex.Message });
            }
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] RegisterRequest request)
        {
            try
            {
                await _authService.SendOtpAsync(request);
                return Ok(new { message = SuccessMessages.OtpSentSuccess });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ErrorMessages.SendOtpError, details = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                var response = await _authService.VerifyOtpAndRegisterAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ErrorMessages.AccountCreationError, details = ex.Message });
            }
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenModel request)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while refreshing the token.", details = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                await _authService.ForgotPasswordAsync(request);
                return Ok(new { message = "Nếu email hợp lệ, mã OTP đã được gửi đến hộp thư của bạn." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xử lý yêu cầu quên mật khẩu.", details = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request);
                return Ok(new { message = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập bằng mật khẩu mới." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xử lý đặt lại mật khẩu.", details = ex.Message });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId) || userId <= 0)
                    return Unauthorized(new { message = "Token không hợp lệ." });

                await _authService.ChangePasswordAsync(userId, request);
                return Ok(new { message = "Mật khẩu đã được thay đổi thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi thay đổi mật khẩu.", details = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId) || userId <= 0)
                    return Unauthorized(new { message = "Token không hợp lệ." });

                await _authService.LogoutAsync(userId);
                return Ok(new { message = "Đăng xuất thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi đăng xuất.", details = ex.Message });
            }
        }
    }
}
