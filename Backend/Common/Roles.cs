namespace Backend.Common;

// IMPORTANT: enum values MUST match dbo.Roles.RoleId seed data.
// Teacher = 1, Student = 2. AuthService.GenerateJwtToken issues claim role = user.RoleId,
// and authorization policies (Program.cs) match against these numeric values.
public enum Roles
{
    Teacher = 1,
    Student = 2
}
