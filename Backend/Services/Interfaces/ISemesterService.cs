using Backend.Common.Models;
using Backend.DTOs.Curriculum.Semester;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Services.Interfaces;

public interface ISemesterService
{
    Task<List<SemesterListItem>> ListAsync(int? status, string? q);
    Task<Result<SemesterDetail>> GetByIdAsync(int id);
    Task<Result<SemesterDetail>> CreateAsync(CreateSemesterRequest request, int userId);
    Task<Result<SemesterDetail>> UpdateAsync(int id, UpdateSemesterRequest request, int userId);
    Task<Result<CloseSemesterResult>> CloseAsync(int id, int adminUserId);
}
