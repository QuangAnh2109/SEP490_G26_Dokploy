using Backend.DTOs;
using Backend.DTOs.Profile;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

using Microsoft.AspNetCore.Identity;

namespace Backend.Services.Implements
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _repo;
        private readonly PasswordHasher<User> _passwordHasher;

        public ProfileService(IProfileRepository repo)
        {
            _repo = repo;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<UserProfileDTO?> GetProfileAsync(int userId)
        {
            var user = await _repo.GetUserByIdAsync(userId);

            if (user == null) return null;

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

        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileDTO dto)
        {
            var user = await _repo.GetUserByIdAsync(userId);

            if (user == null) return false;

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.StudentId = dto.StudentId;

            await _repo.UpdateUserAsync(user);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO dto)
        {
            var user = await _repo.GetUserByIdAsync(userId);

            if (user == null) return false;

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash!,
                dto.CurrentPassword
            );

            if (result == PasswordVerificationResult.Failed)
                return false;

            if (dto.NewPassword != dto.ConfirmPassword)
                return false;

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);

            user.SecurityStamp = DateTime.UtcNow;

            await _repo.UpdateUserAsync(user);
            await _repo.SaveChangesAsync();

            return true;
        }
    }
}