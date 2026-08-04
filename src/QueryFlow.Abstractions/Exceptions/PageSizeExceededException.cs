namespace QueryFlow.Abstractions.Exceptions;

/// <summary>Thrown when a requested page size exceeds the configured maximum.</summary>
public sealed class PageSizeExceededException : QueryFlowException
{
    public int RequestedPageSize { get; }

    public int MaxPageSize { get; }

    public PageSizeExceededException(int requestedPageSize, int maxPageSize)
        : base($"Requested page size {requestedPageSize} exceeds the maximum allowed page size of {maxPageSize}.")
    {
        RequestedPageSize = requestedPageSize;
        MaxPageSize = maxPageSize;
    }
}
