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

        /// <summary>
        /// Trang thống kê nộp bài — danh sách học sinh + lịch sử (dành cho Giáo viên)
        /// </summary>
        [HttpGet]
        public IActionResult ExamSubmitResults(int examId, int? classId = null)
        {
            if (examId <= 0)
                return RedirectToAction("Index", "Home");

            ViewBag.ExamId = examId;
            ViewBag.ClassId = classId;
            return View();
        }

        /// <summary>
        /// Trang xem chi tiết bài làm — dành cho Giáo viên (theo submissionId)
        /// </summary>
        [HttpGet]
        public IActionResult ViewSubmission(int submissionId, int? examId = null, int? classId = null)
        {
            if (submissionId <= 0)
                return RedirectToAction("Index", "Home");

            ViewBag.SubmissionId = submissionId;
            ViewBag.ExamId = examId;
            ViewBag.ClassId = classId;
            return View();
        }
    }
}
