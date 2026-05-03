using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Semester
{
    public int SemesterId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Status { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int UpdatedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;
    public virtual User UpdatedByUser { get; set; } = null!;
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
