using QueryFlow.Expressions;
using QueryFlow.Validation;

namespace QueryFlow.Core;

/// <summary>Adds free-text search to any <see cref="IQueryable{T}"/> over developer-configured searchable fields.</summary>
public static class SearchQueryableExtensions
{
    /// <summary>
    /// Filters <paramref name="source"/> to rows where any field registered via
    /// <see cref="QueryableConfig{T}.Searchable(string)"/> contains <paramref name="searchTerm"/>
    /// (case-insensitive). No-ops when <paramref name="searchTerm"/> is null/blank or no
    /// searchable fields are configured.
    /// </summary>
    public static IQueryable<T> Search<T>(
        this IQueryable<T> source,
        string? searchTerm,
        QueryableConfig<T> config)
    {
        var predicate = SearchExpressionBuilder.Build<T>(searchTerm, config.SearchableFields.ToArray());
        return predicate is null ? source : source.Where(predicate);
    }
}
