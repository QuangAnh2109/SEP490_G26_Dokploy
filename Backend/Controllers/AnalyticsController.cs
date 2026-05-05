using Backend.Common;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Backend.Common.Errors;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AnalyticsController(IAnalyticsService analyticsService, ICurrentUserService currentUserService) : ControllerBase
{
    private readonly IAnalyticsService _analyticsService = analyticsService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    /// <summary>
    /// Phân tích chi tiết bài thi — dành cho Giáo viên.
    /// </summary>
    [HttpGet("exam/{examId}/detail")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> GetExamAnalyticsDetail(int examId)
    {
        var result = await _analyticsService.GetExamAnalyticsDetailAsync(examId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Phân tích bài làm cá nhân — dành cho Học sinh.
    /// </summary>
    [HttpGet("exam/{examId}/student")]
    [Authorize(Roles = RoleIds.Student)]
    public async Task<IActionResult> GetStudentSubmissionAnalytics(int examId)
    {
        int studentId = _currentUserService.UserId;
        var result = await _analyticsService.GetStudentSubmissionAnalyticsAsync(examId, studentId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Thống kê nộp bài — danh sách học sinh + lịch sử nộp (dành cho Giáo viên).
    /// </summary>
    [HttpGet("exam/{examId}/submissions")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> GetExamSubmitResults(int examId)
    {
        var result = await _analyticsService.GetExamSubmitResultsAsync(examId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Xem chi tiết bài làm theo submissionId — dành cho Giáo viên.
    /// </summary>
    [HttpGet("submission/{submissionId}")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> GetSubmissionBySubmissionId(int submissionId)
    {
        var result = await _analyticsService.GetSubmissionBySubmissionIdAsync(submissionId);
        return result.ToActionResult(this);
    }
}
