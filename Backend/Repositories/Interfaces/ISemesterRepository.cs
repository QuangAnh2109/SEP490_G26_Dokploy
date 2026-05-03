using Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Repositories.Interfaces;

public interface ISemesterRepository
{
    Task<List<Semester>> GetAllAsync(int? status, string? q);
    Task<Semester?> GetByIdAsync(int id);
    Task<Semester?> GetByCodeAsync(string code);
    Task AddAsync(Semester semester);
    Task SaveChangesAsync();
}
