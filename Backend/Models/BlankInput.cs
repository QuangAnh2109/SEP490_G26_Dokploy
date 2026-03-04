using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class BlankInput
{
    public int QuestionAnswerId { get; set; }

    public int InputTypeId { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual InputType InputType { get; set; } = null!;

    public virtual QuestionAnswer QuestionAnswer { get; set; } = null!;
}
