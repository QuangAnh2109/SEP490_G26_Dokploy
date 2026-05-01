using Backend.DTOs;

namespace Backend.DTOs.Auth;

public sealed record OtpCacheEntry(RegisterRequest Request, string Otp);
