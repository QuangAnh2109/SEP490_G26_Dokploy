using System.Security.Claims;
using System.Text.Json;

using Backend.Common;
using Backend.DTOs;
using Backend.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers;

[ApiController]
[Route("api/submission")]
public class SubmissionController : ControllerBase
{
    private readonly ISubmissionService _submissionService;

    private readonly ILogger<SubmissionController> _logger;

    public SubmissionController(ISubmissionService submissionService, ILogger<SubmissionController> logger)
    {
        _submissionService = submissionService;
        _logger = logger;
    }

    /// <summary>
    /// Nộp bài kiểm tra. StudentId được lấy từ JWT token.
    /// </summary>
    [HttpPost("submit")]
    [Authorize(Roles = RoleIds.Student)]
    public async Task<ActionResult<SubmitExamResponse>> SubmitExam(
        [FromBody] SubmitExamRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LOG DỮ LIỆU GỬI VỀ ĐỂ NỘP BÀI");
        _logger.LogInformation(JsonSerializer.Serialize(request));
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
