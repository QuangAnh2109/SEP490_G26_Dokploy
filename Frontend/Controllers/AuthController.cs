using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var clientId = (_configuration["Google:ClientId"] ?? _configuration["Google:client_id"] ?? "").Trim();
            ViewData["GoogleClientId"] = clientId;
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            ViewData["Email"] = email;
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ChangePasswordFirstLogin()
        {
            return View();
        }
    }
}
