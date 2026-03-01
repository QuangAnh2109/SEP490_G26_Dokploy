using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class StudentExamController : Controller
    {
        // POST: /StudentExam/TakeExam
        // Hides examId from the URL
        [HttpPost]
        public IActionResult TakeExam(int examId)
        {
            if (examId <= 0)
            {
                // Optionally redirect to an error page or back to the exam list
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ExamId = examId;
            return View();
        }

        // GET: /StudentExam/ExamPreview?examId=1
        [HttpGet]
        public IActionResult ExamPreview(int examId)
        {
            if (examId <= 0) return RedirectToAction("Index", "Home");

            ViewBag.ExamId = examId; 
            return View();
        }

    }
}
