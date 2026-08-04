namespace QueryFlow.Abstractions.Options;

/// <summary>
/// Global, process-wide defaults and guardrails for QueryFlow. Register and customize via
/// <c>services.AddQueryFlow(options => ...)</c> in QueryFlow.AspNetCore, or construct directly
/// for non-DI scenarios.
/// </summary>
public sealed class QueryFlowOptions
{
    /// <summary>Page size used when the client does not specify one. Default 25.</summary>
    public int DefaultPageSize { get; set; } = 25;

    /// <summary>
    /// Hard upper bound on page size, enforced regardless of what the client requests, to
    /// prevent over-fetching. Default 100.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>Maximum number of navigation properties that may be requested via <c>include=</c>. Default 5.</summary>
    public int MaxIncludeDepth { get; set; } = 5;

    /// <summary>Maximum number of filter conditions accepted in a single request. Default 25.</summary>
    public int MaxFilterConditions { get; set; } = 25;

    /// <summary>Maximum number of sort fields accepted in a single request. Default 5.</summary>
    public int MaxSortFields { get; set; } = 5;

    /// <summary>
    /// When true (default), an unknown field name or a field/operator combination outside the
    /// configured allowlist throws. When false, offending clauses are silently ignored.
    /// </summary>
    public bool ThrowOnInvalidField { get; set; } = true;

    /// <summary>Whether free-text search comparisons are case-insensitive. Default true.</summary>
    public bool CaseInsensitiveSearch { get; set; } = true;

    /// <summary>
    /// Secret key material used to sign/encrypt opaque cursor tokens. If not set, a
    /// process-local key is generated at startup — fine for a single instance, but cursors
    /// will not be portable across app restarts or between instances in that case. Set this
    /// explicitly in any multi-instance deployment.
    /// </summary>
    public byte[]? CursorSigningKey { get; set; }
}
