using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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
    /// API lấy phân tích kết quả bài thi theo từng chương (Dành cho Giáo viên)
    /// </summary>
    /// <param name="examId"></param>
    /// <returns></returns>
    [HttpGet("exam/{examId}")]
    [Authorize] // Chỉ Giáo viên mới được xem. Nếu có Role thì thêm Role
    public async Task<IActionResult> GetExamAnalytics(int examId)
    {
        try
        {
            var result = await _analyticsService.GetExamAnalyticsAsync(examId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Log exception here
            return StatusCode(500, new { message = "Lỗi hệ thống khi phân tích dữ liệu bài thi.", details = ex.Message });
        }
    }
}
