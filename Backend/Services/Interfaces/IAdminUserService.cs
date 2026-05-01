using Backend.Common.Models;
using Backend.DTOs.Admin;

namespace Backend.Services.Interfaces;

public interface IAdminUserService
{
    Task<Result<AdminUserListResponse>> ListAsync(AdminUserListQuery query, CancellationToken ct = default);
    Task<Result<AdminUserListItem>> GetAsync(int userId, CancellationToken ct = default);
    Task<Result<CreateUserResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result> LockAsync(int userId, int currentAdminId, CancellationToken ct = default);
    Task<Result> UnlockAsync(int userId, CancellationToken ct = default);
    Task<Result<ResetPasswordResponse>> ResetPasswordAsync(int userId, CancellationToken ct = default);
}
