using Backend.DTOs.StudentExam;
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

        public StudentExamController(IStudentExamService studentExamService)
        {
            _studentExamService = studentExamService;
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

        [HttpGet("{examId}/paper/{paperId}")]
        public async Task<IActionResult> GetExamPaper(int examId, int paperId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            var paper = await _studentExamService.GetExamPaperAsync(studentId, examId, paperId);
            if (paper == null)
            {
                return NotFound("Exam paper not found or access denied.");
            }

            return Ok(paper);
        }

        [HttpPost("submission/start")]
        public async Task<IActionResult> StartSubmission([FromBody] StartSubmissionRequest request)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            var submission = await _studentExamService.StartExamAsync(studentId, request);

            int remainingSeconds = 40 * 60; // default fallback
            if (submission.Paper != null && submission.Paper.Exam != null)
            {
                var durationSeconds = submission.Paper.Exam.Duration * 60;
                var elapsedSeconds = (DateTime.UtcNow - submission.CreatedAtUtc).TotalSeconds;
                
                // Thời gian làm bài còn lại dựa vào duration
                var timeBasedOnDuration = durationSeconds - elapsedSeconds;

                // Thời gian làm bài còn lại dựa vào thời điểm đóng kỳ thi (CloseAt)
                double timeBasedOnCloseAt = double.MaxValue;
                if (submission.Paper.Exam.CloseAt.HasValue)
                {
                    timeBasedOnCloseAt = (submission.Paper.Exam.CloseAt.Value - DateTime.UtcNow).TotalSeconds;
                }

                // Lấy thời gian nhỏ hơn giữa 2 điều kiện
                var actualRemaining = Math.Min(timeBasedOnDuration, timeBasedOnCloseAt);
                remainingSeconds = (int)Math.Max(0, actualRemaining);
            }

            var savedAnswers = submission.StudentAnswers?.Select(a => new 
            {
                questionIndex = a.QuestionIndex,
                responseText = a.ResponseText ?? string.Empty
            }).Cast<object>().ToList() ?? new List<object>();

            var paperDto = await _studentExamService.GetExamPaperAsync(studentId, request.ExamId, submission.PaperId);

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
