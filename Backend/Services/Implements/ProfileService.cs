using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs.Profile;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public class ProfileService(IProfileRepository repo, TimeProvider timeProvider) : IProfileService
{
    public async Task<Result<UserProfileDTO>> GetProfileAsync(int userId)
    {
        var user = await repo.GetUserByIdAsync(userId);
        if (user == null) return ProfileErrors.NotFound;

        return new UserProfileDTO
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            StudentId = user.StudentId,
            RoleId = user.RoleId,
            Status = user.Status
        };
    }

    public async Task<Result> UpdateProfileAsync(int userId, UpdateProfileDTO dto)
    {
        var user = await repo.GetUserByIdAsync(userId);
        if (user == null) return Result.Failure(ProfileErrors.NotFound);

        // Role-dependent StudentId rule (format already validated by FluentValidator)
        if (user.RoleId.ToString() == RoleIds.Student && string.IsNullOrWhiteSpace(dto.StudentId))
            return Result.Failure(ProfileErrors.StudentIdRequired);

        user.FullName = dto.FullName!.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        user.StudentId = string.IsNullOrWhiteSpace(dto.StudentId) ? null : dto.StudentId.Trim();

        await repo.UpdateUserAsync(user);
        await repo.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordDTO dto)
    {
        var user = await repo.GetUserByIdAsync(userId);
        if (user == null) return Result.Failure(ProfileErrors.NotFound);

        if (string.IsNullOrEmpty(user.PasswordHash))
            return Result.Failure(ProfileErrors.GoogleAccount);

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return Result.Failure(ProfileErrors.WrongPassword);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.SecurityStamp = timeProvider.GetUtcNow().UtcDateTime;

        await repo.UpdateUserAsync(user);
        await repo.SaveChangesAsync();
        return Result.Success();
    }
}
