using Backend.DTOs.Curriculum;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
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

    public async Task<(List<Semester> Items, int Total)> GetAllAsync(CurriculumListQuery query)
    {
        var q = _context.Semesters.AsQueryable();

        if (query.Status.HasValue)
        {
            q = q.Where(s => s.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var search = query.Q.Trim().ToLower();
            q = q.Where(s => s.Code.ToLower().Contains(search) || s.Name.ToLower().Contains(search));
        }

        var total = await q.CountAsync();

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var items = await q
            .Include(s => s.Classes)
                .ThenInclude(c => c.Exams)
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.SemesterId)
            .Skip((page - 1) * size)
            .Take(size)
            .AsSplitQuery()
            .ToListAsync();

        return (items, total);
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
