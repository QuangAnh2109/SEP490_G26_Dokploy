using Backend.Common;
using Backend.DTOs.Class;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/class")]
public class ClassController(IClassService service, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("my")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    public async Task<IActionResult> GetMyClasses()
    {
        var classes = await service.GetClassesForUserAsync(currentUser.UserId);
        return Ok(classes);
    }

    [HttpGet("{id}/exams")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    public async Task<IActionResult> GetExamsForClass(int id)
    {
        var result = await service.GetExamsForCurrentUserAsync(id);
        return result.ToActionResult(this);
    }

    [HttpGet("{id}/chapters")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    public async Task<IActionResult> GetChaptersForClass(int id)
    {
        var result = await service.GetChaptersForCurrentUserAsync(id);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/leave")]
    [Authorize(Roles = RoleIds.Student)]
    public async Task<IActionResult> LeaveClass(int id)
    {
        var result = await service.LeaveClassAsync(id, currentUser.UserId);
        return result.ToActionResult(this);
    }

    [HttpPost("join")]
    [Authorize(Roles = RoleIds.Student)]
    public async Task<IActionResult> JoinClass([FromBody] JoinClassRequestDTO request)
    {
        var result = await service.JoinClassAsync(currentUser.UserId, request.InvitationCode);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequestDTO request)
    {
        var result = await service.CreateClassAsync(currentUser.UserId, request);
        return result.ToActionResult(this);
    }

    [HttpGet("{id}/students")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    public async Task<IActionResult> GetStudentsInClass(int id)
    {
        var students = await service.GetStudentsInClassAsync(id);
        return Ok(students);
    }

    [HttpGet("{id}/settings")]
    [Authorize(Roles = RoleIds.Teacher + "," + RoleIds.Student)]
    public async Task<IActionResult> GetClassSettings(int id)
    {
        var result = await service.GetClassSettingsAsync(id);
        return result.ToActionResult(this);
    }

    [HttpPut("{id}/settings")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> UpdateClassSettings(int id, [FromBody] UpdateClassSettingsRequestDTO request)
    {
        var result = await service.UpdateClassSettingsAsync(id, request.ClassName, request.InvitationCodeStatus);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/invite")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> InviteStudent(int id, [FromBody] InviteStudentRequestDTO request)
    {
        var result = await service.InviteStudentByEmailAsync(currentUser.UserId, id, request.Email);
        return result.ToActionResult(this);
    }

    [HttpPost("accept-invite")]
    [Authorize(Roles = RoleIds.Student)]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequestDTO request)
    {
        var result = await service.AcceptInvitationAsync(currentUser.UserId, request.Token);
        return result.ToActionResult(this);
    }

    [HttpGet("{id}/students/pending")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> GetPendingStudents(int id)
    {
        var students = await service.GetPendingStudentsAsync(id);
        return Ok(students);
    }

    [HttpPost("{id}/students/{studentId}/approve")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> ApproveStudent(int id, int studentId)
    {
        var result = await service.ApproveStudentAsync(id, studentId);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id}/students/{studentId}/reject")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> RejectStudent(int id, int studentId)
    {
        var result = await service.RejectStudentAsync(id, studentId);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id}/students/{studentId}/remove")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> RemoveStudent(int id, int studentId)
    {
        var result = await service.RemoveStudentAsync(id, studentId);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/close")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> CloseClass(int id)
    {
        var result = await service.CloseClassAsync(id);
        return result.ToActionResult(this);
    }

    [HttpPost("{id}/reopen")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> ReopenClass(int id)
    {
        var result = await service.ReopenClassAsync(id);
        return result.ToActionResult(this);
    }

    [HttpGet("subjects")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> GetSubjects()
    {
        var result = await service.GetSubjectsAsync();
        return Ok(result);
    }

    [HttpGet("semesters")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> ListSemesters()
    {
        var result = await service.GetSemestersAsync();
        return Ok(result);
    }
}