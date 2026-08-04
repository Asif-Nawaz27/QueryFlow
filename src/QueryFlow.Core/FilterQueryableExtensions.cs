using QueryFlow.Abstractions.Models;
using QueryFlow.Expressions;
using QueryFlow.Validation;

namespace QueryFlow.Core;

/// <summary>Adds dynamic filtering to any <see cref="IQueryable{T}"/>.</summary>
public static class FilterQueryableExtensions
{
    /// <summary>
    /// Applies a flat list of filter conditions (ANDed/ORed per each condition's
    /// <see cref="FilterCondition.Connector"/>). Fields and operators are validated against
    /// <paramref name="config"/> (or a wide-open default when omitted) before touching the query.
    /// </summary>
    public static IQueryable<T> Filter<T>(
        this IQueryable<T> source,
        IReadOnlyList<FilterCondition> filters,
        QueryableConfig<T>? config = null)
    {
        if (filters.Count == 0)
        {
            return source;
        }

        config ??= QueryableConfig<T>.Default;
        ValidateConditions(filters, config);

        var predicate = FilterExpressionBuilder.Build<T>(filters);
        return source.Where(predicate);
    }

    /// <summary>Applies a nested <see cref="FilterGroup"/> tree, enabling arbitrary AND/OR grouping.</summary>
    public static IQueryable<T> Filter<T>(
        this IQueryable<T> source,
        FilterGroup group,
        QueryableConfig<T>? config = null)
    {
        if (group.IsEmpty)
        {
            return source;
        }

        config ??= QueryableConfig<T>.Default;
        ValidateGroup(group, config);

        var predicate = FilterExpressionBuilder.Build<T>(group);
        return source.Where(predicate);
    }

    /// <summary>Applies whichever filter shape is present on <paramref name="request"/> (<see cref="QueryRequest.FilterGroup"/> takes precedence over <see cref="QueryRequest.Filters"/>).</summary>
    public static IQueryable<T> Filter<T>(
        this IQueryable<T> source,
        QueryRequest request,
        QueryableConfig<T>? config = null) =>
        request.FilterGroup is not null
            ? source.Filter(request.FilterGroup, config)
            : source.Filter(request.Filters, config);

    private static void ValidateGroup<T>(FilterGroup group, QueryableConfig<T> config)
    {
        ValidateConditions(group.Conditions, config);

        foreach (var nested in group.Groups)
        {
            ValidateGroup(nested, config);
        }
    }

    private static void ValidateConditions<T>(IReadOnlyList<FilterCondition> conditions, QueryableConfig<T> config)
    {
        foreach (var condition in conditions)
        {
            condition.EnsureWellFormed();
            if (!config.IsFilterAllowed(condition.Field, condition.Operator))
            {
                throw config.IsKnownField(condition.Field)
                    ? new Abstractions.Exceptions.InvalidOperatorException(condition.Field, condition.Operator)
                    : new Abstractions.Exceptions.InvalidFieldException(condition.Field);
            }
        }
    }
}
