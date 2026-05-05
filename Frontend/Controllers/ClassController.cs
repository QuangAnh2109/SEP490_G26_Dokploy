using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class ClassController : Controller
    {
        [HttpGet]
        public IActionResult ExamListInClass(int id)
        {
            ViewBag.ClassId = id;
            ViewBag.ClassName = (string?)Request.Query["className"];
            return View();
        }

        [HttpGet]
        public IActionResult StudentList(int id)
        {
            ViewBag.ClassId = id;
            ViewBag.ClassName = (string?)Request.Query["className"];
            return View();
        }

        [HttpGet]
        public IActionResult Settings(int id)
        {
            ViewBag.ClassId = id;
            ViewBag.ClassName = (string?)Request.Query["className"];
            return View();
        }

        public IActionResult ClassList()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Join(string code)
        {
            ViewBag.InviteCode = code;
            return View();
        }

        [HttpGet]
        public IActionResult AcceptInvite(string token)
        {
            ViewBag.Token = token;
            return View();
        }

        [HttpGet]
        public IActionResult PendingStudents(int id)
        {
            ViewBag.ClassId = id;
            return View();
        }

        [HttpGet]
        public IActionResult ExamAnalytics(int examId)
        {
            // Do Frontend call trực tiếp từ Browser qua JS fetch
            // Nên Controller MVC chỉ cần hứng ID để gài vào View
            ViewBag.ExamId = examId;
            return View();
        }
    }
}