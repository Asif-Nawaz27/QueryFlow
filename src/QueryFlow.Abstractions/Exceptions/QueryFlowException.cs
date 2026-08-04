namespace QueryFlow.Abstractions.Exceptions;

/// <summary>Base type for all exceptions thrown by QueryFlow.</summary>
public abstract class QueryFlowException : Exception
{
    protected QueryFlowException(string message) : base(message)
    {
    }

    protected QueryFlowException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
