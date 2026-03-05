using Backend.DTOs.StudentExam;
using Backend.Helpers;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/student/exams")]
    [ApiController]
    [Authorize(Roles = "Student,Học sinh")]
    public class StudentExamController : ControllerBase
    {
        private readonly IStudentExamService _studentExamService;

        private readonly ILogger<StudentExamController> _logger;

        public StudentExamController(IStudentExamService studentExamService, ILogger<StudentExamController> logger)
        {
            _studentExamService = studentExamService;
            _logger = logger;
        }

        private int GetStudentId()
        {
            var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return 0; 
        }

        [HttpGet("{examId}/paper")]
        public async Task<IActionResult> GetExamPaper(string examId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            var decryptedExamId = SecureIdHelper.DecryptId(examId);
            if (decryptedExamId == null)
            {
                return BadRequest("Invalid exam ID.");
            }

            _logger.LogInformation("Student {StudentId} is requesting exam paper for exam {ExamId}", studentId, decryptedExamId);
            try
            {
                var result = await _studentExamService.TakeExamInClass(decryptedExamId.Value, studentId);
                if (result == null)
                {
                    return NotFound("Exam paper not found or access denied.");
                }
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam paper");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{examId}/take")]
        public async Task<IActionResult> TakeExamInClass(string examId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            //var decryptedExamId = SecureIdHelper.DecryptId(examId);
            //if (decryptedExamId == null)
            //{
            //    return BadRequest("Invalid exam ID.");
            //}

            var decryptedExamId = int.Parse(examId);

            try
            {
                var result = await _studentExamService.TakeExamInClass(decryptedExamId, studentId);
                if (result == null)
                {
                    return NotFound("Bài thi không tồn tại hoặc chưa mở.");
                }
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TakeExamInClass");
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        [HttpPost("submission/start")]
        public async Task<IActionResult> StartSubmission([FromBody] StartSubmissionRequest request)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            Submission submission;
            try
            {
                submission = await _studentExamService.StartExamAsync(studentId, request);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("|ACTIVE_EXAM_ID:"))
                {
                    var parts = ex.Message.Split("|ACTIVE_EXAM_ID:");
                    var message = parts[0];
                    var activeExamId = int.Parse(parts[1]);
                    return BadRequest(new { message = message, activeExamId = activeExamId });
                }
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }

            int remainingSeconds = 40 * 60; // default fallback
            if (submission.Paper != null && submission.Paper.Exam != null)
            {
                var durationSeconds = submission.Paper.Exam.Duration * 60;
                var elapsedSeconds = (DateTime.UtcNow - submission.CreatedAtUtc).TotalSeconds;
                
                var timeBasedOnDuration = durationSeconds - elapsedSeconds;

                double timeBasedOnCloseAt = double.MaxValue;
                if (submission.Paper.Exam.CloseAt.HasValue)
                {
                    timeBasedOnCloseAt = (submission.Paper.Exam.CloseAt.Value - DateTime.UtcNow).TotalSeconds;
                }

                var actualRemaining = Math.Min(timeBasedOnDuration, timeBasedOnCloseAt);
                remainingSeconds = (int)Math.Max(0, actualRemaining);
            }

            var savedAnswers = submission.StudentAnswers?.Select(a => new 
            {
                questionAnswerId = a.QuestionAnswerId,
                response = a.Response ?? string.Empty
            }).Cast<object>().ToList() ?? new List<object>();

            var paperDto = await _studentExamService.TakeExamInClass(request.ExamId, studentId);

            return Ok(new 
            { 
                submissionId = submission.SubmissionId, 
                paperId = submission.PaperId, 
                paper = paperDto,
                status = "Started",
                remainingSeconds = remainingSeconds,
                savedAnswers = savedAnswers
            });
        }

        [HttpPost("submission/{submissionId}/answer")]
        public async Task<IActionResult> SaveAnswer(int submissionId, [FromBody] SubmitAnswerRequest request)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            await _studentExamService.SaveAnswerAsync(studentId, submissionId, request);
            return Ok(new { message = "Answer saved successfully." });
        }

        [HttpPost("submission/{submissionId}/answers/batch")]
        public async Task<IActionResult> SaveBulkAnswers(int submissionId, [FromBody] List<SubmitAnswerRequest> requests)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            await _studentExamService.SaveBulkAnswersAsync(studentId, submissionId, requests);
            return Ok(new { message = "Answers bulk saved successfully." });
        }

        [HttpPost("submission/{submissionId}/submit")]
        public async Task<IActionResult> SubmitExam(int submissionId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            await _studentExamService.SubmitExamAsync(studentId, submissionId);
            return Ok(new { message = "Exam submitted successfully." });
        }

        [HttpGet("{examId}/preview")]
        public async Task<IActionResult> GetExamPreview(int examId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            try
            {
                var preview = await _studentExamService.GetExamPreviewAsync(studentId, examId);
                if (preview == null)
                    return NotFound("Exam not found.");

                return Ok(preview);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }
    }
}
