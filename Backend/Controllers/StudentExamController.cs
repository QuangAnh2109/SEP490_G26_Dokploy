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
    [Authorize(Roles = "Student,Học sinh,Teacher,Giáo viên")]
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

        [HttpPost("{examId}/take")]
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
        [HttpGet("{examId}/preview")]
        public async Task<IActionResult> GetExamPreview(int examId)
        {
            var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                return Unauthorized("Invalid token.");

            var isTeacher = User.IsInRole("Teacher") || User.IsInRole("Giáo viên");

            try
            {
                var preview = await _studentExamService.GetExamPreviewAsync(userId, examId, isTeacher);
                if (preview == null)
                    return NotFound("Exam not found.");

                return Ok(preview);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        /// <summary>
        /// Lấy lịch sử bài nộp tổng hợp (kiểm tra + luyện tập) của sinh viên.
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetSubmissionHistory([FromQuery] int? classId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var result = await _studentExamService.GetAllSubmissionHistoryAsync(studentId, classId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission history for student {StudentId}", studentId);
                return StatusCode(500, new { message = "Lỗi hệ thống khi tải lịch sử bài nộp." });
            }
        }

    }
}
