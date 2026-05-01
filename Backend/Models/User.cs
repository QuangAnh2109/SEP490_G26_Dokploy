using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public int RoleId { get; set; }

    public int Status { get; set; }

    public DateTime SecurityStamp { get; set; }

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? StudentId { get; set; }

    public bool MustChangePassword { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<ExamBlueprint> ExamBlueprints { get; set; } = new List<ExamBlueprint>();

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
