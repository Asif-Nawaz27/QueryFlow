namespace QueryFlow.Abstractions.Exceptions;

/// <summary>
/// Thrown when a request references a field that does not exist on the target type or is not
/// present in the configured allowlist.
/// </summary>
public sealed class InvalidFieldException : QueryFlowException
{
    public string Field { get; }

    public InvalidFieldException(string field)
        : base($"Field '{field}' is unknown or not allowed for this query.")
    {
        Field = field;
    }
}
