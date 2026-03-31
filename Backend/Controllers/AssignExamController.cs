using Backend.DTOs;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/assign-exam")]
public class AssignExamController : ControllerBase
{
    private readonly IAssignExamService _assignExamService;

    public AssignExamController(IAssignExamService assignExamService)
    {
        _assignExamService = assignExamService;
    }

    //[HttpGet("filters")]
    //public async Task<ActionResult<AssignExamFiltersResponseDto>> GetFilters(
    //    [FromQuery] int? teacherId,
    //    CancellationToken cancellationToken = default)
    //{
    //    try
    //    {
    //        var result = await _assignExamService.GetFiltersAsync(teacherId, cancellationToken);
    //        return Ok(result);
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //    catch (KeyNotFoundException ex)
    //    {
    //        return NotFound(new { message = ex.Message });
    //    }
    //}

    //[HttpGet("classes")]
    //public async Task<ActionResult<PagedResultDto<ClassListItemDto>>> GetClasses(
    //    [FromQuery] int? teacherId,
    //    [FromQuery] string? keyword,
    //    [FromQuery] string? subjectCode,
    //    [FromQuery] string? semester,
    //    [FromQuery] int page = 1,
    //    [FromQuery] int pageSize = 10,
    //    CancellationToken cancellationToken = default)
    //{
    //    var result = await _assignExamService.GetClassesAsync(
    //        teacherId, keyword, subjectCode, semester, page, pageSize, cancellationToken);
    //    return Ok(result);
    //}

    [HttpGet("blueprints")]
    public async Task<ActionResult<IReadOnlyList<BlueprintListItemDto>>> GetBlueprints(
        [FromQuery] int? teacherId,
        [FromQuery] string? subjectCode,
        [FromQuery] string? keyword,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _assignExamService.GetBlueprintsAsync(
                teacherId, subjectCode, keyword, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("blueprints/{id:int}/detail")]
    public async Task<ActionResult<IReadOnlyList<BlueprintDetailRowDto>>> GetBlueprintDetail(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.GetBlueprintDetailAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("questions")]
    public async Task<ActionResult<IReadOnlyList<QuestionListItemDto>>> GetQuestions(
        [FromQuery] int? teacherId,
        [FromQuery] string? subjectCode,
        [FromQuery] int? chapterId,
        [FromQuery] int? difficulty,
        CancellationToken cancellationToken = default)
    {
        var result = await _assignExamService.GetQuestionsAsync(
            teacherId, subjectCode, chapterId, difficulty, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreateAssignExamResponse>> CreateAssignExam(
        [FromBody] CreateAssignExamRequest request,
        CancellationToken cancellationToken = default)
    {
        //
        try
        {
            var result = await _assignExamService.CreateAssignExamAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("review/{id:int}")]
    public async Task<ActionResult<ExamReviewDto>> GetExamReview(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _assignExamService.GetExamReviewAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("papers/{paperId:int}/questions/{questionId:int}/alternatives")]
    public async Task<ActionResult<IReadOnlyList<QuestionListItemDto>>> GetAlternativeQuestions(
        [FromRoute] int paperId,
        [FromRoute] int questionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _assignExamService.GetAlternativeQuestionsAsync(paperId, questionId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("swap-question")]
    public async Task<ActionResult> SwapQuestion(
        [FromBody] SwapQuestionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _assignExamService.SwapPaperQuestionAsync(request, cancellationToken);
            return Ok(new { message = "Question swapped successfully." });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("approve/{id:int}")]
    public async Task<ActionResult> ApproveExam(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _assignExamService.ApproveExamAsync(id, cancellationToken);
            return Ok(new { message = "Exam approved successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
