using Backend.DTOs;
using Backend.DTOs.Auth;

namespace Backend.Services.Interfaces;

public interface IOtpStore
{
    Task SetRegistrationOtpAsync(string email, RegisterRequest request, string otp, CancellationToken ct = default);
    Task<OtpCacheEntry?> GetRegistrationOtpAsync(string email, CancellationToken ct = default);
    Task RemoveRegistrationOtpAsync(string email, CancellationToken ct = default);

    Task SetResetOtpAsync(string email, string otp, CancellationToken ct = default);
    Task<string?> GetResetOtpAsync(string email, CancellationToken ct = default);
    Task RemoveResetOtpAsync(string email, CancellationToken ct = default);
}
