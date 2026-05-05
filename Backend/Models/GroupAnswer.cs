using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class GroupAnswer
{
    public int GroupAnswerId { get; set; }

    public string Name { get; set; } = null!;

    public int? DependsOnGroupId { get; set; }

    public virtual GroupAnswer? DependsOnGroup { get; set; }

    public virtual ICollection<GroupAnswer> InverseDependsOnGroup { get; set; } = new List<GroupAnswer>();

    public virtual ICollection<QuestionAnswer> QuestionAnswers { get; set; } = new List<QuestionAnswer>();
}
