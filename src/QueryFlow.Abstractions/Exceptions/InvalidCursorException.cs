namespace QueryFlow.Abstractions.Exceptions;

/// <summary>Thrown when a cursor token fails to decode, fails signature verification, or is malformed.</summary>
public sealed class InvalidCursorException : QueryFlowException
{
    public InvalidCursorException(string message) : base(message)
    {
    }

    public InvalidCursorException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
