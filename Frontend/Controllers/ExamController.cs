using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class ExamController : Controller
    {
        public IActionResult AssignExam()
        {
            return View("assign_exam");
        }

        public IActionResult ExamReview([FromQuery] int examId, [FromQuery] int? classId)
        {
            ViewBag.ExamId = examId;
            ViewBag.ClassId = classId;
            return View("ExamReview");
        }
    }
}
