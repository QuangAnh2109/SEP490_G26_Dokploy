using Backend.DTOs.Admin;
using Backend.Models;

namespace Backend.Repositories.Interfaces;

public interface IAdminUserRepository
{
    Task<(List<User> Items, int Total)> ListAsync(AdminUserListQuery query, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int userId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateStatusAsync(int userId, int status, CancellationToken ct = default);
    Task UpdatePasswordAsync(int userId, string passwordHash, bool mustChange, CancellationToken ct = default);
}
