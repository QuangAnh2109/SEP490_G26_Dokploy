using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class InputType
{
    public int InputTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Regex { get; set; } = null!;

    public string? GroupType { get; set; }

    public virtual ICollection<QuestionAnswer> QuestionAnswers { get; set; } = new List<QuestionAnswer>();
}
