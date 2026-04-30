using Backend.Common;
using Backend.DTOs;
using Backend.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/submission")]
public class SubmissionController(ISubmissionService submissionService) : ControllerBase
{
    [HttpPost("submit")]
    [Authorize(Roles = RoleIds.Student)]
    public async Task<IActionResult> SubmitExam(
        [FromBody] SubmitExamRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await submissionService.SubmitExamAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }
}
