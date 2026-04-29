using Backend.Common.Models;
using Backend.DTOs.Profile;

namespace Backend.Services.Interfaces;

public interface IProfileService
{
    Task<Result<UserProfileDTO>> GetProfileAsync(int userId);
    Task<Result> UpdateProfileAsync(int userId, UpdateProfileDTO dto);
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordDTO dto);
}
