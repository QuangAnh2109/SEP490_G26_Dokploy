using Backend.Common;
using Backend.Common.Models;
using Backend.DTOs;

namespace Backend.Services.Interfaces;

public interface ISubmissionService
{
    Task<Result<SubmitExamResponse>> SubmitExamAsync(SubmitExamRequest request, CancellationToken ct = default);
}
