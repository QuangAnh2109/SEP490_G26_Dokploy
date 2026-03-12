
using Backend.DTOs.Profile;

namespace Backend.Services.Interfaces
{
    public interface IProfileService
    {
        Task<UserProfileDTO?> GetProfileAsync(int userId);

        Task<bool> UpdateProfileAsync(int userId, UpdateProfileDTO dto);

        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO dto);
    }
}