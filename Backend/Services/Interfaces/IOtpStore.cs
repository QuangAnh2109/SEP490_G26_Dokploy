namespace Backend.Services.Interfaces;

public interface IOtpStore
{
    Task SetResetOtpAsync(string email, string otp, CancellationToken ct = default);
    Task<string?> GetResetOtpAsync(string email, CancellationToken ct = default);
    Task RemoveResetOtpAsync(string email, CancellationToken ct = default);
}
