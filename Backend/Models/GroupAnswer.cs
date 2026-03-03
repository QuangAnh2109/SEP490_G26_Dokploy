using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class GroupAnswer
{
    public int GroupAnswerId { get; set; }

    public string Name { get; set; } = null!;

    public string AnswersContent { get; set; } = null!;

    public virtual ICollection<QuestionAnswer> QuestionAnswers { get; set; } = new List<QuestionAnswer>();
}
