using Backend.Common;
using Backend.DTOs;
using Backend.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers;

[ApiController]
[Route("api/submission")]
public class SubmissionController(
    ISubmissionService submissionService,
    ILogger<SubmissionController> logger) : ControllerBase
{
    /// <summary>
    /// Nộp bài kiểm tra. StudentId được lấy từ JWT token.
    /// </summary>
    [HttpPost("submit")]
    [Authorize(Roles = RoleIds.Student)]
    public async Task<IActionResult> SubmitExam(
        [FromBody] SubmitExamRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("LOG DỮ LIỆU GỬI VỀ ĐỂ NỘP BÀI");
        // logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(request));

        var result = await submissionService.SubmitExamAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }
}
