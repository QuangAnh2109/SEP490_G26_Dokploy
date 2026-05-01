namespace Backend.Constants;

// Values MUST match dbo.Users.Status seed (DEFAULT 1 = Active).
// Locked accounts (Status=0) are rejected at login with AUTH_ACCOUNT_LOCKED;
// admin sets via /api/admin/users/{id}/lock which also revokes refresh tokens.
public static class UserStatus
{
    public const int Active = 1;
    public const int Locked = 0;
}
