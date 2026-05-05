using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Paper
{
    public int PaperId { get; set; }

    public int? ExamId { get; set; }

    public int? Code { get; set; }

    public virtual Exam? Exam { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
