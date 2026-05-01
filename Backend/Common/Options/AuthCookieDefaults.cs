namespace Backend.Common.Options;

public static class AuthCookieDefaults
{
    public const string Name = "mtca.auth";
    public const string RefreshName = "mtca.rt";

    /// <summary>Bắt buộc true để chống XSS đọc token từ document.cookie.</summary>
    public const bool HttpOnly = true;
}
