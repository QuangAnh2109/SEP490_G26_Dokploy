using Backend.DTOs.PracticeExam;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/practice")]
    [ApiController]
    [Authorize(Roles = "Student,Học sinh")]
    public class PracticeExamController : ControllerBase
    {
        private readonly IPracticeExamService _practiceService;
        private readonly ILogger<PracticeExamController> _logger;

        public PracticeExamController(IPracticeExamService practiceService, ILogger<PracticeExamController> logger)
        {
            _practiceService = practiceService;
            _logger = logger;
        }

        private int GetStudentId()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out int id) ? id : 0;
        }

        // ════════════════════════════════════════════════════════
        //  GET /api/practice/class/{classId}/chapters
        //  Lấy danh sách chương + proficiency + số câu khả dụng
        // ════════════════════════════════════════════════════════
        /// <summary>
        /// Lấy danh sách chương của khóa học kèm proficiency và số câu luyện tập có sẵn.
        /// </summary>
        [HttpGet("class/{classId}/chapters")]
        public async Task<IActionResult> GetChaptersForPractice(int classId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var result = await _practiceService.GetChaptersForPracticeAsync(classId, studentId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters for practice. ClassId={ClassId}", classId);
                return StatusCode(500, new { message = "Lỗi hệ thống." });
            }
        }

        // ════════════════════════════════════════════════════════
        //  POST /api/practice/create
        //  Tạo đề luyện tập tự động
        // ════════════════════════════════════════════════════════
        /// <summary>
        /// Tạo đề luyện tập tự động dựa trên proficiency sinh viên.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePracticeExam([FromBody] CreatePracticeExamRequest request)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var result = await _practiceService.CreatePracticeExamAsync(studentId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating practice exam for student {StudentId}", studentId);
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo đề luyện tập." });
            }
        }

        // ════════════════════════════════════════════════════════
        //  POST /api/practice/submit
        //  Nộp bài luyện tập (không check thời gian)
        // ════════════════════════════════════════════════════════
        /// <summary>
        /// Nộp bài luyện tập — không giới hạn thời gian.
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitPracticeExam([FromBody] SubmitPracticeExamRequest request)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var result = await _practiceService.SubmitPracticeExamAsync(studentId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting practice exam. SubmissionId={SubmissionId}", request.SubmissionId);
                return StatusCode(500, new { message = "Lỗi hệ thống khi nộp bài luyện tập." });
            }
        }

        // ════════════════════════════════════════════════════════
        //  POST /api/practice/save
        //  Lưu câu trả lời giữa chừng (không nộp bài)
        // ════════════════════════════════════════════════════════
        /// <summary>
        /// Lưu câu trả lời giữa chừng — không nộp bài, giữ trạng thái InProgress.
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> SavePracticeAnswers([FromBody] SubmitPracticeExamRequest request)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                await _practiceService.SavePracticeAnswersAsync(studentId, request);
                return Ok(new { message = "Đã lưu câu trả lời." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving practice answers. SubmissionId={SubmissionId}", request.SubmissionId);
                return StatusCode(500, new { message = "Lỗi hệ thống khi lưu câu trả lời." });
            }
        }

        // ════════════════════════════════════════════════════════
        //  GET /api/practice/resume/{submissionId}
        //  Quay lại bài luyện tập đang làm dở
        // ════════════════════════════════════════════════════════
        /// <summary>
        /// Resume bài luyện tập đang làm dở — trả lại câu hỏi + câu trả lời đã lưu.
        /// </summary>
        [HttpGet("resume/{submissionId}")]
        public async Task<IActionResult> ResumePracticeExam(int submissionId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var result = await _practiceService.ResumePracticeExamAsync(submissionId, studentId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming practice exam. SubmissionId={SubmissionId}", submissionId);
                return StatusCode(500, new { message = "Lỗi hệ thống." });
            }
        }

        // ════════════════════════════════════════════════════════
        //  GET /api/practice/result/{submissionId}
        //  Xem kết quả + đáp án từng câu
        // ════════════════════════════════════════════════════════
        /// <summary>
        /// Xem kết quả bài luyện tập — hiển thị đáp án từng câu.
        /// </summary>
        [HttpGet("result/{submissionId}")]
        public async Task<IActionResult> GetPracticeResult(int submissionId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var result = await _practiceService.GetPracticeResultAsync(submissionId, studentId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting practice result. SubmissionId={SubmissionId}", submissionId);
                return StatusCode(500, new { message = "Lỗi hệ thống." });
            }
        }

        // ════════════════════════════════════════════════════════
        //  GET /api/practice/history?classId=
        //  Lịch sử luyện tập
        // ════════════════════════════════════════════════════════
        /// <summary>
        /// Lấy lịch sử luyện tập. Có thể lọc theo classId.
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetPracticeHistory([FromQuery] int? classId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var result = await _practiceService.GetPracticeHistoryAsync(studentId, classId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting practice history for student {StudentId}", studentId);
                return StatusCode(500, new { message = "Lỗi hệ thống." });
            }
        }
    }
}
