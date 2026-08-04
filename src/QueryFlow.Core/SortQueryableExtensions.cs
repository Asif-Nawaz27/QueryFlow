using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Expressions;
using QueryFlow.Validation;

namespace QueryFlow.Core;

/// <summary>Adds dynamic multi-column sorting to any <see cref="IQueryable{T}"/>.</summary>
public static class SortQueryableExtensions
{
    public static IQueryable<T> Sort<T>(
        this IQueryable<T> source,
        IReadOnlyList<SortField> sortFields,
        QueryableConfig<T>? config = null)
    {
        if (sortFields.Count == 0)
        {
            return source;
        }

        config ??= QueryableConfig<T>.Default;
        foreach (var field in sortFields)
        {
            if (!config.IsSortAllowed(field.Field))
            {
                throw new InvalidFieldException(field.Field);
            }
        }

        return SortExpressionBuilder.Apply(source, sortFields);
    }

    /// <summary>Parses tokens like <c>"LastName,-CreatedDate"</c> and applies them as a sort.</summary>
    public static IQueryable<T> Sort<T>(
        this IQueryable<T> source,
        string? sortExpression,
        QueryableConfig<T>? config = null)
    {
        if (string.IsNullOrWhiteSpace(sortExpression))
        {
            return source;
        }

        var fields = sortExpression
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SortField.Parse)
            .ToArray();

        return source.Sort(fields, config);
    }
}
