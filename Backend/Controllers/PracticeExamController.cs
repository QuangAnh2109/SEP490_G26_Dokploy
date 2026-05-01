using Backend.Common;
using Backend.DTOs.PracticeExam;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/practice")]
    [ApiController]
    public class PracticeExamController(IPracticeExamService practiceService) : ControllerBase
    {
        private readonly IPracticeExamService _practiceService = practiceService;

        /// <summary>
        /// Lấy danh sách chương của khóa học kèm proficiency và số câu luyện tập có sẵn.
        /// </summary>
        [HttpGet("class/{classId}/chapters")]
        [Authorize(Roles = RoleIds.Student)]
        public async Task<IActionResult> GetChaptersForPractice(int classId)
        {
            var result = await _practiceService.GetChaptersForPracticeAsync(classId);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Tạo đề luyện tập tự động dựa trên proficiency sinh viên.
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = RoleIds.Student)]
        public async Task<IActionResult> CreatePracticeExam([FromBody] CreatePracticeExamRequest request)
        {
            var result = await _practiceService.CreatePracticeExamAsync(request);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Nộp bài luyện tập — không giới hạn thời gian.
        /// </summary>
        [HttpPost("submit")]
        [Authorize(Roles = RoleIds.Student)]
        public async Task<IActionResult> SubmitPracticeExam([FromBody] SubmitPracticeExamRequest request)
        {
            var result = await _practiceService.SubmitPracticeExamAsync(request);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Lưu câu trả lời giữa chừng — không nộp bài, giữ trạng thái InProgress.
        /// </summary>
        [HttpPost("save")]
        [Authorize(Roles = RoleIds.Student)]
        public async Task<IActionResult> SavePracticeAnswers([FromBody] SubmitPracticeExamRequest request)
        {
            var result = await _practiceService.SavePracticeAnswersAsync(request);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Resume bài luyện tập đang làm dở — trả lại câu hỏi + câu trả lời đã lưu.
        /// </summary>
        [HttpGet("resume/{submissionId}")]
        [Authorize(Roles = RoleIds.Student)]
        public async Task<IActionResult> ResumePracticeExam(int submissionId)
        {
            var result = await _practiceService.ResumePracticeExamAsync(submissionId);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Xem kết quả bài luyện tập — hiển thị đáp án từng câu.
        /// </summary>
        [HttpGet("result/{submissionId}")]
        [Authorize(Roles = RoleIds.Student)]
        public async Task<IActionResult> GetPracticeResult(int submissionId)
        {
            var result = await _practiceService.GetPracticeResultAsync(submissionId);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Lấy lịch sử luyện tập. Có thể lọc theo classId.
        /// </summary>
        [HttpGet("history")]
        [Authorize(Roles = RoleIds.Student)]
        public async Task<IActionResult> GetPracticeHistory([FromQuery] int? classId)
        {
            var result = await _practiceService.GetPracticeHistoryAsync(classId);
            return result.ToActionResult(this);
        }
    }
}
