namespace Backend.Common.Models;

public sealed record Error(string Code, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, ErrorType.Unexpected);
}
