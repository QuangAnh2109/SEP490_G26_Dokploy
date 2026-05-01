namespace Backend.DTOs.Admin;

public class AdminUserListQuery
{
    public string? Q { get; set; }
    public int? RoleId { get; set; }
    public int? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
