namespace Backend.Common;

// RoleIds values MUST match dbo.Roles.RoleId seed data: Teacher=1, Student=2.
// Use these constants in [Authorize(Roles = ...)] attributes and JWT claim comparisons.
public static class RoleIds
{
    public const string Teacher = "1";
    public const string Student = "2";
}
