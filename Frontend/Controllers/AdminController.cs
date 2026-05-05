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

    [HttpGet("Semesters")]
    public IActionResult Semesters()
    {
        ViewBag.Breadcrumb = new[] { ("Kỳ học", (string?)null) };
        return View();
    }

    [HttpGet("Subjects")]
    public IActionResult Subjects()
    {
        ViewBag.Breadcrumb = new[] { ("Môn học", (string?)null) };
        return View();
    }

    [HttpGet("SubjectDetail/{id}")]
    public IActionResult SubjectDetail(int id)
    {
        ViewBag.Breadcrumb = new[] { 
            ("Môn học", "/Admin/Subjects"),
            ("Chi tiết môn học", (string?)null)
        };
        ViewBag.SubjectId = id;
        return View();
    }
}
