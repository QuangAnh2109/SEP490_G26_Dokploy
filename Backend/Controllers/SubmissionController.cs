using Backend.Common;
using Backend.DTOs;
using Backend.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/submission")]
public class SubmissionController(
    ISubmissionService submissionService,
    IGradingService gradingService) : ControllerBase
{
    [HttpPost("submit")]
    [Authorize(Roles = RoleIds.Student)]
    public async Task<IActionResult> SubmitExam(
        [FromBody] SubmitExamRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await submissionService.SubmitExamAsync(request, cancellationToken);

        if (result.IsSuccess && request.Submit == true)
        {
            // Cố ý KHÔNG truyền cancellationToken: nếu client cancel sau khi đã submit,
            // chấm điểm vẫn phải hoàn tất để tránh kẹt submission ở GradingStatus dở.
            await gradingService.GradeSubmissionAsync(result.Value.SubmissionId);
        }

        return result.ToActionResult(this);
    }
}
