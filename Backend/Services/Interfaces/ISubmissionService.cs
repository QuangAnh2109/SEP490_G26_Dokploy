using Backend.DTOs;

namespace Backend.Services.Interfaces;

public interface ISubmissionService
{
    Task<SubmitExamResponse> SubmitExamAsync(int studentId, SubmitExamRequest request, CancellationToken ct = default);
}
