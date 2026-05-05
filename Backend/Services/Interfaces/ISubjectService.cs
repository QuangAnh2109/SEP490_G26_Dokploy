using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Common.Models;
using Backend.DTOs;
using Backend.DTOs.Curriculum;
using Backend.DTOs.Curriculum.Subject;

namespace Backend.Services.Interfaces;

public interface ISubjectService
{
    Task<Result<PagedResultDto<SubjectListItem>>> ListAsync(CurriculumListQuery query);
    Task<Result<SubjectDetail>> GetAsync(int id);
    Task<Result<SubjectDetail>> CreateAsync(CreateSubjectRequest request, int adminUserId);
    Task<Result<SubjectDetail>> UpdateAsync(int id, UpdateSubjectRequest request, int adminUserId);
    Task<Result> CloseAsync(int id, int adminUserId);
}
