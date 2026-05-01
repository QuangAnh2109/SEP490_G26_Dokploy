namespace Backend.Common;

// RoleIds values MUST match dbo.Roles.RoleId seed data: Teacher=1, Student=2, Admin=3.
// String forms for [Authorize(Roles = ...)] + JWT claim comparison.
// Int forms for User.RoleId (DB column is INT).
public static class RoleIds
{
    public const string Teacher = "1";
    public const string Student = "2";
    public const string Admin   = "3";

    public const int TeacherInt = 1;
    public const int StudentInt = 2;
    public const int AdminInt   = 3;

    public static bool IsValid(int roleId) => roleId is TeacherInt or StudentInt or AdminInt;

    public static string GetName(int roleId) => roleId switch
    {
        TeacherInt => "Teacher",
        StudentInt => "Student",
        AdminInt   => "Admin",
        _          => "Unknown"
    };
}
