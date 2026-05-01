using Backend.Common.Options;
using Backend.Constants;

namespace Backend.Common;

public static class CookieHelper
{
    public static void SetAccessCookie(HttpResponse response, string token, DateTimeOffset expiresAt, AuthCookieOptions opts) =>
        response.Cookies.Append(AuthCookieDefaults.Name, token, BuildOptions(opts, path: "/", expiresAt));

    public static void SetRefreshCookie(HttpResponse response, string token, DateTimeOffset expiresAt, AuthCookieOptions opts) =>
        response.Cookies.Append(AuthCookieDefaults.RefreshName, token, BuildOptions(opts, path: AuthRoutes.RefreshTokenPath, expiresAt));

    public static void ClearAccessCookie(HttpResponse response, AuthCookieOptions opts) =>
        response.Cookies.Delete(AuthCookieDefaults.Name, BuildDeleteOptions(opts, path: "/"));

    public static void ClearRefreshCookie(HttpResponse response, AuthCookieOptions opts) =>
        response.Cookies.Delete(AuthCookieDefaults.RefreshName, BuildDeleteOptions(opts, path: AuthRoutes.RefreshTokenPath));

    public static void ClearAuthCookies(HttpResponse response, AuthCookieOptions opts)
    {
        ClearAccessCookie(response, opts);
        ClearRefreshCookie(response, opts);
    }

    private static CookieOptions BuildOptions(AuthCookieOptions opts, string path, DateTimeOffset expiresAt) => new()
    {
        HttpOnly = AuthCookieDefaults.HttpOnly,
        Secure = opts.Secure,
        SameSite = ParseSameSite(opts.SameSite),
        Domain = string.IsNullOrWhiteSpace(opts.Domain) ? null : opts.Domain,
        Path = path,
        Expires = expiresAt,
        IsEssential = true
    };

    private static CookieOptions BuildDeleteOptions(AuthCookieOptions opts, string path) => new()
    {
        HttpOnly = AuthCookieDefaults.HttpOnly,
        Secure = opts.Secure,
        SameSite = ParseSameSite(opts.SameSite),
        Domain = string.IsNullOrWhiteSpace(opts.Domain) ? null : opts.Domain,
        Path = path
    };

    private static SameSiteMode ParseSameSite(string value) => value?.Trim().ToLowerInvariant() switch
    {
        "strict" => SameSiteMode.Strict,
        "none" => SameSiteMode.None,
        _ => SameSiteMode.Lax
    };
}
