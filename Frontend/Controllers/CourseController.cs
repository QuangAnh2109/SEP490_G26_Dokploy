using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class CourseController : Controller
    {
        [HttpGet]
        public IActionResult ExamListInCourse(int id)
        {
            ViewBag.ClassId = id;
            return View();
        }

        public IActionResult CourseList()
        {
            return View();
        }
    }
}