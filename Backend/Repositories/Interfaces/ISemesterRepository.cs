using Backend.DTOs.Curriculum;
using Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Repositories.Interfaces;

public interface ISemesterRepository
{
    Task<(List<Semester> Items, int Total)> GetAllAsync(CurriculumListQuery query);
    Task<Semester?> GetByIdAsync(int id);
    Task<Semester?> GetByCodeAsync(string code);
    Task AddAsync(Semester semester);
    Task SaveChangesAsync();
}
