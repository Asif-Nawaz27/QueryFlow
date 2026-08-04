namespace QueryFlow.Abstractions.Models;

/// <summary>
/// The result of a cursor-paginated query. Cursors are opaque, encoded tokens produced by
/// QueryFlow.Core — clients should treat them as strings and round-trip them verbatim.
/// </summary>
public sealed class CursorPagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int PageSize { get; init; }

    /// <summary>Cursor to pass as <c>?cursor=</c> to fetch the next page, or null if this is the last page.</summary>
    public string? NextCursor { get; init; }

    /// <summary>Cursor to pass as <c>?cursor=</c> to fetch the previous page, or null if this is the first page.</summary>
    public string? PreviousCursor { get; init; }

    public bool HasNext => NextCursor is not null;

    public bool HasPrevious => PreviousCursor is not null;
}
