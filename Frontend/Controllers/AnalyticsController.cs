using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class AnalyticsController : Controller
    {
        /// <summary>
        /// Trang phân tích bài thi — dành cho Giáo viên
        /// </summary>
        [HttpGet]
        public IActionResult ExamAnalytics(int examId)
        {
            if (examId <= 0)
                return RedirectToAction("Index", "Home");

            ViewBag.ExamId = examId;
            return View();
        }

        /// <summary>
        /// Trang xem lại bài làm + phân tích — dành cho Học sinh
        /// </summary>
        [HttpGet]
        public IActionResult StudentResult(int examId)
        {
            if (examId <= 0)
                return RedirectToAction("Index", "Home");

            ViewBag.ExamId = examId;
            return View();
        }
    }
}
