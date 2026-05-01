using Backend.Common;
using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs.Admin;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace Backend.Services.Implements;

public class AdminUserService(
    IAdminUserRepository adminUserRepository,
    IRefreshTokenStore refreshTokenStore,
    IEmailService emailService,
    TimeProvider timeProvider) : IAdminUserService
{
    public async Task<Result<AdminUserListResponse>> ListAsync(AdminUserListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await adminUserRepository.ListAsync(query, ct);
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        return new AdminUserListResponse
        {
            Items = items.Select(MapToListItem).ToList(),
            Total = total,
            Page = page,
            PageSize = size
        };
    }

    public async Task<Result<AdminUserListItem>> GetAsync(int userId, CancellationToken ct = default)
    {
        var user = await adminUserRepository.GetByIdAsync(userId, ct);
        if (user == null) return AdminUserErrors.NotFound;
        return MapToListItem(user);
    }

    public async Task<Result<CreateUserResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (request.RoleId != RoleIds.TeacherInt && request.RoleId != RoleIds.StudentInt)
            return AdminUserErrors.InvalidRole;

        if (await adminUserRepository.EmailExistsAsync(request.Email!, ct))
            return AdminUserErrors.EmailExists;

        var tempPassword = GenerateTempPassword();
        var user = new User
        {
            Email = request.Email!,
            FullName = (request.FullName ?? string.Empty).Trim(),
            RoleId = request.RoleId!.Value,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            MustChangePassword = true,
            Status = UserStatus.Active,
            SecurityStamp = timeProvider.GetUtcNow().UtcDateTime,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            StudentId = string.IsNullOrWhiteSpace(request.StudentId) ? null : request.StudentId.Trim()
        };

        try
        {
            await emailService.SendEmailAsync(
                request.Email!,
                "Tài khoản MTCA của bạn",
                BuildWelcomeEmail(request.Email!, tempPassword));
        }
        catch
        {
            return AdminUserErrors.EmailSendFailed;
        }

        var created = await adminUserRepository.AddAsync(user, ct);
        return new CreateUserResponse { UserId = created.UserId, Email = created.Email };
    }

    public async Task<Result> LockAsync(int userId, int currentAdminId, CancellationToken ct = default)
    {
        if (userId == currentAdminId) return AdminUserErrors.CannotLockSelf;

        var user = await adminUserRepository.GetByIdAsync(userId, ct);
        if (user == null) return AdminUserErrors.NotFound;
        if (user.RoleId == RoleIds.AdminInt) return AdminUserErrors.CannotModifyAdmin;

        await adminUserRepository.UpdateStatusAsync(userId, UserStatus.Locked, ct);
        await refreshTokenStore.RevokeAllAsync(userId);
        return Result.Success();
    }

    public async Task<Result> UnlockAsync(int userId, CancellationToken ct = default)
    {
        var user = await adminUserRepository.GetByIdAsync(userId, ct);
        if (user == null) return AdminUserErrors.NotFound;
        if (user.RoleId == RoleIds.AdminInt) return AdminUserErrors.CannotModifyAdmin;

        await adminUserRepository.UpdateStatusAsync(userId, UserStatus.Active, ct);
        return Result.Success();
    }

    public async Task<Result<ResetPasswordResponse>> ResetPasswordAsync(int userId, CancellationToken ct = default)
    {
        var user = await adminUserRepository.GetByIdAsync(userId, ct);
        if (user == null) return AdminUserErrors.NotFound;
        if (user.RoleId == RoleIds.AdminInt) return AdminUserErrors.CannotModifyAdmin;

        var tempPassword = GenerateTempPassword();

        try
        {
            await emailService.SendEmailAsync(
                user.Email,
                "Mật khẩu mới MTCA của bạn",
                BuildResetPasswordEmail(user.Email, tempPassword));
        }
        catch
        {
            return AdminUserErrors.EmailSendFailed;
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
        await adminUserRepository.UpdatePasswordAsync(userId, hash, mustChange: true, ct);
        await refreshTokenStore.RevokeAllAsync(userId);

        return new ResetPasswordResponse { Email = user.Email };
    }

    private static AdminUserListItem MapToListItem(User u) => new()
    {
        UserId = u.UserId,
        Email = u.Email,
        FullName = u.FullName,
        RoleId = u.RoleId,
        RoleName = u.Role?.Name ?? RoleIds.GetName(u.RoleId),
        Status = u.Status,
        MustChangePassword = u.MustChangePassword
    };

    private static string GenerateTempPassword()
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghjkmnpqrstuvwxyz";
        const string digits  = "23456789";
        const string special = "@#$!%*?&";
        const string all     = upper + lower + digits + special;

        var bytes = RandomNumberGenerator.GetBytes(16);
        var sb = new StringBuilder(12);

        // Guarantee one from each group
        sb.Append(upper[bytes[0] % upper.Length]);
        sb.Append(lower[bytes[1] % lower.Length]);
        sb.Append(digits[bytes[2] % digits.Length]);
        sb.Append(special[bytes[3] % special.Length]);

        // Fill remaining 8 chars from full pool
        for (int i = 4; i < 12; i++)
            sb.Append(all[bytes[i] % all.Length]);

        // Shuffle using cryptographic bytes (.NET 8 native)
        var chars = sb.ToString().ToCharArray();
        RandomNumberGenerator.Shuffle(chars.AsSpan());

        return new string(chars);
    }

    private static string BuildWelcomeEmail(string email, string tempPassword) => $@"
        <div style='font-family: Arial, sans-serif; padding: 20px;'>
            <h2>Chào mừng đến với Math Test Creator</h2>
            <p>Tài khoản của bạn đã được tạo. Thông tin đăng nhập:</p>
            <ul>
                <li><strong>Email:</strong> {email}</li>
                <li><strong>Mật khẩu tạm:</strong> <code style='font-size:1.1em'>{HtmlEncoder.Default.Encode(tempPassword)}</code></li>
            </ul>
            <p>Bạn sẽ được yêu cầu đổi mật khẩu ngay sau lần đăng nhập đầu tiên.</p>
        </div>";

    private static string BuildResetPasswordEmail(string email, string tempPassword) => $@"
        <div style='font-family: Arial, sans-serif; padding: 20px;'>
            <h2>Mật khẩu của bạn đã được đặt lại</h2>
            <p>Quản trị viên đã cấp lại mật khẩu cho tài khoản <strong>{email}</strong>.</p>
            <p><strong>Mật khẩu tạm:</strong> <code style='font-size:1.1em'>{HtmlEncoder.Default.Encode(tempPassword)}</code></p>
            <p>Bạn sẽ được yêu cầu đổi mật khẩu ngay sau lần đăng nhập tiếp theo.</p>
        </div>";
}
