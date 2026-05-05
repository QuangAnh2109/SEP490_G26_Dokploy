namespace Backend.DTOs.Analytics;

public class StudentRankDto
{
    public string StudentName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public decimal Score { get; set; }
}
