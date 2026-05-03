using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class PracticeExamController : Controller
    {
        // GET: /PracticeExam/Setup?classId=5
        [HttpGet]
        public IActionResult Setup(int classId)
        {
            if (classId <= 0) return RedirectToAction("ClassList", "Class");
            ViewBag.ClassId = classId;
            return View();
        }

        // GET: /PracticeExam/TakePractice?submissionId=123
        [HttpGet]
        public IActionResult TakePractice(int submissionId)
        {
            if (submissionId <= 0) return RedirectToAction("ClassList", "Class");
            ViewBag.SubmissionId = submissionId;
            return View();
        }

        // GET: /PracticeExam/Result?submissionId=123
        [HttpGet]
        public IActionResult Result(int submissionId)
        {
            if (submissionId <= 0) return RedirectToAction("ClassList", "Class");
            ViewBag.SubmissionId = submissionId;
            return View();
        }

        // GET: /PracticeExam/History?classId=5
        [HttpGet]
        public IActionResult History(int classId)
        {
            if (classId <= 0) return RedirectToAction("ClassList", "Class");
            ViewBag.ClassId = classId;
            return View();
        }
    }
}
