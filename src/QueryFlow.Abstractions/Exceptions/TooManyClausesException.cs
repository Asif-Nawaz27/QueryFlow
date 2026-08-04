namespace QueryFlow.Abstractions.Exceptions;

/// <summary>
/// Thrown when a request contains more filter conditions or sort fields than
/// <see cref="Options.QueryFlowOptions.MaxFilterConditions"/> / <see cref="Options.QueryFlowOptions.MaxSortFields"/> allow.
/// </summary>
public sealed class TooManyClausesException : QueryFlowException
{
    public string ClauseKind { get; }

    public int Requested { get; }

    public int Max { get; }

    public TooManyClausesException(string clauseKind, int requested, int max)
        : base($"Request contains {requested} {clauseKind}, exceeding the maximum of {max}.")
    {
        ClauseKind = clauseKind;
        Requested = requested;
        Max = max;
    }
}
