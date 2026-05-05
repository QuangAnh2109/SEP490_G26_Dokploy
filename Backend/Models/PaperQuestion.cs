using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class PaperQuestion
{
    public int PaperId { get; set; }

    public int QuestionId { get; set; }

    public virtual Paper Paper { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;
}
