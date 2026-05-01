using Backend.DTOs.Admin;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements;

public class AdminUserRepository(MtcaSep490G26Context context) : IAdminUserRepository
{
    public async Task<(List<User> Items, int Total)> ListAsync(AdminUserListQuery query, CancellationToken ct = default)
    {
        var q = context.Users.Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            q = q.Where(u => u.Email.Contains(term) ||
                              (u.FullName != null && u.FullName.Contains(term)));
        }

        if (query.RoleId.HasValue)
            q = q.Where(u => u.RoleId == query.RoleId.Value);

        if (query.Status.HasValue)
            q = q.Where(u => u.Status == query.Status.Value);

        var total = await q.CountAsync(ct);

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var items = await q
            .OrderByDescending(u => u.UserId)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken ct = default) =>
        context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync(ct);
        await context.Entry(user).Reference(u => u.Role).LoadAsync(ct);
        return user;
    }

    public async Task UpdateStatusAsync(int userId, int status, CancellationToken ct = default)
    {
        await context.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, status), ct);
    }

    public async Task UpdatePasswordAsync(int userId, string passwordHash, bool mustChange, CancellationToken ct = default)
    {
        await context.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PasswordHash, passwordHash)
                .SetProperty(u => u.MustChangePassword, mustChange), ct);
    }
}
