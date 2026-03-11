using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Phân tích chi tiết bài thi — dành cho Giáo viên.
    /// </summary>
    [HttpGet("exam/{examId}/detail")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetExamAnalyticsDetail(int examId)
    {
        try
        {
            var result = await _analyticsService.GetExamAnalyticsDetailAsync(examId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống khi phân tích chi tiết bài thi.", details = ex.Message });
        }
    }

    /// <summary>
    /// Phân tích bài làm cá nhân — dành cho Học sinh.
    /// </summary>
    [HttpGet("exam/{examId}/student")]
    [Authorize(Roles = "Student,Học sinh")]
    public async Task<IActionResult> GetStudentSubmissionAnalytics(int examId)
    {
        try
        {
            var userIdString = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var studentId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            var result = await _analyticsService.GetStudentSubmissionAnalyticsAsync(examId, studentId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống khi phân tích bài làm.", details = ex.Message });
        }
    }
}
