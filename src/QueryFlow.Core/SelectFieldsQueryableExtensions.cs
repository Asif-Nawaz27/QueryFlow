using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Expressions;
using QueryFlow.Validation;

namespace QueryFlow.Core;

/// <summary>Adds client-driven field selection (<c>?fields=id,name,email</c>) to any <see cref="IQueryable{T}"/>.</summary>
public static class SelectFieldsQueryableExtensions
{
    /// <summary>
    /// Projects <paramref name="source"/> onto only the requested fields. Providers such as EF
    /// Core translate this into a SQL query that selects just those columns, preventing
    /// over-fetching. Returns <paramref name="source"/> unchanged when <paramref name="fields"/> is empty.
    /// </summary>
    public static IQueryable<object> SelectFields<T>(
        this IQueryable<T> source,
        IReadOnlyList<string> fields,
        QueryableConfig<T>? config = null)
        where T : class
    {
        if (fields.Count == 0)
        {
            return source;
        }

        config ??= QueryableConfig<T>.Default;
        foreach (var field in fields)
        {
            if (!config.IsSelectAllowed(field))
            {
                throw new InvalidFieldException(field);
            }
        }

        return FieldSelectionExpressionBuilder.Apply(source, fields);
    }

    /// <summary>Converts a materialized field-selection projection into a plain, JSON-friendly dictionary.</summary>
    public static IReadOnlyDictionary<string, object?> ToFieldDictionary(this object projectedItem) =>
        FieldSelectionExpressionBuilder.ToDictionary(projectedItem);
}
