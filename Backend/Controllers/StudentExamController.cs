using Backend.DTOs.StudentExam;
using Backend.Common;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/student/exams")]
[ApiController]
public class StudentExamController(
    IStudentExamService studentExamService) : ControllerBase
{
    [HttpPost("{examId}/take")]
    [Authorize(Roles = RoleIds.Student)]
    public async Task<IActionResult> TakeExamInClass(int examId)
    {
        var result = await studentExamService.TakeExamInClass(examId);
        return result.ToActionResult(this);
    }

    [HttpGet("{examId}/preview")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    public async Task<IActionResult> GetExamPreview(int examId)
    {
        var result = await studentExamService.GetExamPreviewAsync(examId);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Lấy lịch sử bài nộp tổng hợp (kiểm tra + luyện tập) của sinh viên.
    /// </summary>
    [HttpGet("history")]
    [Authorize(Roles = RoleIds.Student)]
    public async Task<IActionResult> GetSubmissionHistory([FromQuery] int? classId)
    {
        var result = await studentExamService.GetAllSubmissionHistoryAsync(classId);
        return result.ToActionResult(this);
    }
}
