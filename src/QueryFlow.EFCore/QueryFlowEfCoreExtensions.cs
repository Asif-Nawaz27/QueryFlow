using QueryFlow.Abstractions.Models;
using QueryFlow.Core;
using QueryFlow.Validation;

namespace QueryFlow.EFCore;

/// <summary>One-call composition of filter + search + sort + include for EF Core sources.</summary>
public static class QueryFlowEfCoreExtensions
{
    /// <summary>
    /// Applies filtering, free-text search, sorting and eager loading from <paramref name="request"/>
    /// in one call. Field selection and pagination are intentionally left out — call
    /// <c>SelectFields</c>/<c>PaginateAsync</c>/<c>CursorPaginateAsync</c> from QueryFlow.Core
    /// afterwards, since those change the element type or materialize the query.
    /// </summary>
    public static IQueryable<T> ApplyQuery<T>(
        this IQueryable<T> source,
        QueryRequest request,
        QueryableConfig<T>? config = null)
        where T : class
    {
        config ??= QueryableConfig<T>.Default;

        return source
            .Filter(request, config)
            .Search(request.Search, config)
            .Sort(request.Sort, config)
            .Include(request.Includes, config);
    }
}
