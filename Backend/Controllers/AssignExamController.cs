using Backend.Common;
using Backend.DTOs;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/assign-exam")]
public class AssignExamController(IAssignExamService assignExamService) : ControllerBase
{
    private readonly IAssignExamService _assignExamService = assignExamService;

    [HttpGet("blueprints")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> GetBlueprints(
        [FromQuery] string? subjectCode,
        [FromQuery] string? keyword,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.GetBlueprintsAsync(
            subjectCode, keyword, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("blueprints/{id:int}/detail")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> GetBlueprintDetail(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.GetBlueprintDetailAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("questions")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] string? subjectCode,
        [FromQuery] int? chapterId,
        [FromQuery] int? difficulty,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.GetQuestionsAsync(
            subjectCode, chapterId, difficulty, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> CreateAssignExam(
        [FromBody] CreateAssignExamRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.CreateAssignExamAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("review/{id:int}")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> GetExamReview(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.GetExamReviewAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("papers/{paperId:int}/questions/{questionId:int}/alternatives")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> GetAlternativeQuestions(
        [FromRoute] int paperId,
        [FromRoute] int questionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.GetAlternativeQuestionsAsync(paperId, questionId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("swap-question")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> SwapQuestion(
        [FromBody] SwapQuestionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.SwapPaperQuestionAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("approve/{id:int}")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> ApproveExam(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.ApproveExamAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("cancel/{id:int}")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> CancelExam(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.CancelExamAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("restore/{id:int}")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> RestoreExam(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.RestoreExamAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> DeleteExam(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.DeleteExamAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("{id:int}/info")]
    [Authorize(Roles = RoleIds.Teacher)]
    public async Task<IActionResult> UpdateExamInfo(
        [FromRoute] int id,
        [FromBody] UpdateExamInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.UpdateExamInfoAsync(id, request, cancellationToken);
        return result.ToActionResult(this);
    }
}
