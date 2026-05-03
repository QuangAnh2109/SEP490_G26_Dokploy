using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Common.Models;
using Backend.DTOs.Curriculum.Subject;

namespace Backend.Services.Interfaces;

public interface ISubjectService
{
    Task<Result<List<SubjectListItem>>> ListAsync(int? status, string? q);
    Task<Result<SubjectDetail>> GetAsync(int id);
    Task<Result<SubjectDetail>> CreateAsync(CreateSubjectRequest request, int adminUserId);
    Task<Result<SubjectDetail>> UpdateAsync(int id, UpdateSubjectRequest request, int adminUserId);
    Task<Result> CloseAsync(int id, int adminUserId);
}
