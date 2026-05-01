using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

[Route("[controller]")]
public class AdminController : Controller
{
    [HttpGet("Login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet("Users")]
    public IActionResult Users()
    {
        ViewBag.Breadcrumb = new[] { ("Tài khoản người dùng", (string?)null) };
        return View();
    }

    [HttpGet("CreateUser")]
    public IActionResult CreateUser()
    {
        ViewBag.Breadcrumb = new[] { 
            ("Tài khoản người dùng", "/Admin/Users"),
            ("Tạo tài khoản", (string?)null)
        };
        return View();
    }

    [HttpGet("UserDetail/{id}")]
    public IActionResult UserDetail(int id)
    {
        ViewBag.Breadcrumb = new[] { 
            ("Tài khoản người dùng", "/Admin/Users"),
            ("Chi tiết tài khoản", (string?)null)
        };
        ViewBag.UserId = id;
        return View();
    }
}
