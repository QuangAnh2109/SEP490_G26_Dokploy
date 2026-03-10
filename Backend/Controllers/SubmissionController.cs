using System.Security.Claims;
using Backend.DTOs;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/submission")]
[Authorize]
public class SubmissionController : ControllerBase
{
    private readonly ISubmissionService _submissionService;

    public SubmissionController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    /// <summary>
    /// Nộp bài kiểm tra. StudentId được lấy từ JWT token.
    /// </summary>
    [HttpPost("submit")]
    public async Task<ActionResult<SubmitExamResponse>> SubmitExam(
        [FromBody] SubmitExamRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Lấy studentId từ JWT token
            var userIdString = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var studentId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            var result = await _submissionService.SubmitExamAsync(
                studentId, request, cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
