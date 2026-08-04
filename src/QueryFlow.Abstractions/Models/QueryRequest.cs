namespace QueryFlow.Abstractions.Models;

/// <summary>
/// An immutable, transport-agnostic description of a dynamic query: filtering, searching,
/// sorting, field selection, includes, aggregates and pagination. Build one from a query
/// string (via QueryFlow.AspNetCore) or a JSON body and feed it into the <c>Filter</c>,
/// <c>Search</c>, <c>Sort</c>, <c>SelectFields</c> and <c>Paginate</c> extension methods.
/// </summary>
public sealed record QueryRequest
{
    public static readonly IReadOnlyList<FilterCondition> NoFilters = Array.Empty<FilterCondition>();
    public static readonly IReadOnlyList<SortField> NoSort = Array.Empty<SortField>();
    public static readonly IReadOnlyList<string> NoFields = Array.Empty<string>();
    public static readonly IReadOnlyList<AggregateRequest> NoAggregates = Array.Empty<AggregateRequest>();

    /// <summary>Flat list of filter conditions, combined per each condition's <see cref="FilterCondition.Connector"/>.</summary>
    public IReadOnlyList<FilterCondition> Filters { get; init; } = NoFilters;

    /// <summary>Optional nested filter tree. When set, takes precedence over <see cref="Filters"/>.</summary>
    public FilterGroup? FilterGroup { get; init; }

    /// <summary>Free-text search term applied across the configured searchable columns.</summary>
    public string? Search { get; init; }

    /// <summary>Ordered list of sort fields.</summary>
    public IReadOnlyList<SortField> Sort { get; init; } = NoSort;

    /// <summary>Fields to project. Empty means "all fields".</summary>
    public IReadOnlyList<string> Fields { get; init; } = NoFields;

    /// <summary>Navigation properties to eagerly load.</summary>
    public IReadOnlyList<string> Includes { get; init; } = NoFields;

    /// <summary>Aggregate functions to compute alongside (or instead of) the paged result.</summary>
    public IReadOnlyList<AggregateRequest> Aggregates { get; init; } = NoAggregates;

    /// <summary>1-based page number for offset pagination. Null selects cursor pagination when <see cref="Cursor"/> is set.</summary>
    public int? Page { get; init; }

    /// <summary>Number of items per page. Null defers to <see cref="Options.QueryFlowOptions.DefaultPageSize"/>.</summary>
    public int? PageSize { get; init; }

    /// <summary>Opaque cursor token for cursor-based pagination.</summary>
    public string? Cursor { get; init; }

    /// <summary>True when a cursor was supplied, selecting cursor pagination over offset pagination.</summary>
    public bool UseCursorPagination => !string.IsNullOrEmpty(Cursor);

    public static QueryRequest Empty { get; } = new();
}
