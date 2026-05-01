namespace Backend.DTOs.Admin;

public class AdminUserListResponse
{
    public List<AdminUserListItem> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
