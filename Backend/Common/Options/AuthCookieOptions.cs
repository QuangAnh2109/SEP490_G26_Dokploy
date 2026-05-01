namespace Backend.Common.Options;

public sealed class AuthCookieOptions
{
    public const string SectionName = "AuthCookie";

    public bool Secure { get; set; }
    public string SameSite { get; set; } = default!;
    public string? Domain { get; set; }
}
