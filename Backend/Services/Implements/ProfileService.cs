using Backend.DTOs;
using Backend.DTOs.Profile;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Backend.Constants;

namespace Backend.Services.Implements
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _repo;
        public ProfileService(IProfileRepository repo)
        {
            _repo = repo;
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

            var fullName = (dto.FullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                throw new InvalidOperationException(ValidationMessages.FullNameRequired);
            if (!System.Text.RegularExpressions.Regex.IsMatch(fullName, @"^[\p{L}\p{M}]+(?:\s+[\p{L}\p{M}]+)*$"))
                throw new InvalidOperationException(ValidationMessages.FullNameInvalid);

            var phone = (dto.PhoneNumber ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(phone) &&
                !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$"))
                throw new InvalidOperationException(ValidationMessages.PhoneNumberInvalid);

            string? studentId = null;
            if (user.RoleId == 2)
            {
                studentId = (dto.StudentId ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(studentId))
                    throw new InvalidOperationException(ValidationMessages.StudentIdRequiredForStudent);
                if (!System.Text.RegularExpressions.Regex.IsMatch(studentId, @"^[A-Za-z]{2}\d{6}$"))
                    throw new InvalidOperationException(ValidationMessages.StudentIdInvalid);
            }
            else
            {
                var raw = (dto.StudentId ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(raw) &&
                    !System.Text.RegularExpressions.Regex.IsMatch(raw, @"^[A-Za-z]{2}\d{6}$"))
                    throw new InvalidOperationException(ValidationMessages.StudentIdInvalid);
                studentId = string.IsNullOrWhiteSpace(raw) ? null : raw;
            }

            user.FullName = fullName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone;
            user.StudentId = studentId;

            await _repo.UpdateUserAsync(user);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO dto)
        {
            var user = await _repo.GetUserByIdAsync(userId);

            if (user == null) return false;

            if (string.IsNullOrEmpty(user.PasswordHash))
                return false;

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            user.SecurityStamp = DateTime.UtcNow;

            await _repo.UpdateUserAsync(user);
            await _repo.SaveChangesAsync();

            return true;
        }
    }
}