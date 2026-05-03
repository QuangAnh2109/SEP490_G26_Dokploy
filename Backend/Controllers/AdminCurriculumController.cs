using Backend.Common;
using Backend.Constants;
using Backend.DTOs.Curriculum.Semester;
using Backend.DTOs.Curriculum.Subject;

using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Controllers;

[Route("api/admin/curriculum")]
[ApiController]
public class AdminCurriculumController : ControllerBase
{
    private readonly ISemesterService _semesterService;
    private readonly ISubjectService _subjectService;
    private readonly ICurrentUserService _currentUserService;

    public AdminCurriculumController(
        ISemesterService semesterService,
        ISubjectService subjectService,
        ICurrentUserService currentUserService)
    {
        _semesterService = semesterService;
        _subjectService = subjectService;
        _currentUserService = currentUserService;
    }

    [HttpGet("semesters")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<ActionResult<List<SemesterListItem>>> ListSemestersAsync([FromQuery] int? status, [FromQuery] string? q)
    {
        var result = await _semesterService.ListAsync(status, q);
        return Ok(result);
    }

    [HttpGet("semesters/{id:int}")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<IActionResult> GetSemesterAsync(int id)
    {
        var result = await _semesterService.GetByIdAsync(id);
        return result.ToActionResult(this);
    }

    [HttpPost("semesters")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<IActionResult> CreateSemesterAsync([FromBody] CreateSemesterRequest request)
    {
        var result = await _semesterService.CreateAsync(request, _currentUserService.UserId);
        return result.IsSuccess ? StatusCode(201, result.Value) : result.ToActionResult(this);
    }

    [HttpPut("semesters/{id:int}")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<IActionResult> UpdateSemesterAsync(int id, [FromBody] UpdateSemesterRequest request)
    {
        var result = await _semesterService.UpdateAsync(id, request, _currentUserService.UserId);
        return result.ToActionResult(this);
    }

    [HttpPatch("semesters/{id:int}/close")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<IActionResult> CloseSemesterAsync(int id)
    {
        var result = await _semesterService.CloseAsync(id, _currentUserService.UserId);
        return result.ToActionResult(this);
    }

    // ----- Subjects -----
    [HttpGet("subjects")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<IActionResult> ListSubjectsAsync([FromQuery] int? status, [FromQuery] string? q)
    {
        var result = await _subjectService.ListAsync(status, q);
        return result.ToActionResult(this);
    }

    [HttpGet("subjects/{id:int}")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<IActionResult> GetSubjectAsync(int id)
    {
        var result = await _subjectService.GetAsync(id);
        return result.ToActionResult(this);
    }

    [HttpPost("subjects")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<IActionResult> CreateSubjectAsync([FromBody] CreateSubjectRequest request)
    {
        var result = await _subjectService.CreateAsync(request, _currentUserService.UserId);
        return result.IsSuccess ? StatusCode(201, result.Value) : result.ToActionResult(this);
    }

    [HttpPut("subjects/{id:int}")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<IActionResult> UpdateSubjectAsync(int id, [FromBody] UpdateSubjectRequest request)
    {
        var result = await _subjectService.UpdateAsync(id, request, _currentUserService.UserId);
        return result.ToActionResult(this);
    }

    [HttpPatch("subjects/{id:int}/close")]
    [Authorize(Roles = RoleIds.Admin)]
    public async Task<IActionResult> CloseSubjectAsync(int id)
    {
        var result = await _subjectService.CloseAsync(id, _currentUserService.UserId);
        return result.ToActionResult(this);
    }
}
