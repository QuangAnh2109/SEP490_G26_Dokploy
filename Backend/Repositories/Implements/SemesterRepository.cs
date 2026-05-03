using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Repositories.Implements;

public class SemesterRepository : ISemesterRepository
{
    private readonly MtcaSep490G26Context _context;

    public SemesterRepository(MtcaSep490G26Context context)
    {
        _context = context;
    }

    public async Task<List<Semester>> GetAllAsync(int? status, string? q)
    {
        var query = _context.Semesters
            .Include(s => s.Classes)
                .ThenInclude(c => c.Exams)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            query = query.Where(s => s.Code.ToLower().Contains(search) || s.Name.ToLower().Contains(search));
        }

        return await query.OrderByDescending(s => s.StartDate).ThenByDescending(s => s.SemesterId).ToListAsync();
    }

    public async Task<Semester?> GetByIdAsync(int id)
    {
        return await _context.Semesters
            .Include(s => s.CreatedByUser)
            .Include(s => s.UpdatedByUser)
            .Include(s => s.Classes)
                .ThenInclude(c => c.Exams)
            .FirstOrDefaultAsync(s => s.SemesterId == id);
    }

    public async Task<Semester?> GetByCodeAsync(string code)
    {
        var lowerCode = code.ToLower();
        return await _context.Semesters.FirstOrDefaultAsync(s => s.Code.ToLower() == lowerCode);
    }

    public async Task AddAsync(Semester semester)
    {
        _context.Semesters.Add(semester);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
