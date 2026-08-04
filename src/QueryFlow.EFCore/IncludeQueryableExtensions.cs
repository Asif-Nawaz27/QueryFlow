using Microsoft.EntityFrameworkCore;
using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Validation;

namespace QueryFlow.EFCore;

/// <summary>Adds client-driven eager loading (<c>?include=customer,address</c>) to any EF Core <see cref="IQueryable{T}"/>.</summary>
public static class IncludeQueryableExtensions
{
    /// <summary>
    /// Eagerly loads each requested navigation property path (dotted paths like
    /// <c>"Orders.Items"</c> are supported, same as EF Core's string-based <c>Include</c>).
    /// Paths are validated against <paramref name="config"/> before being spliced into the query.
    /// </summary>
    public static IQueryable<T> Include<T>(
        this IQueryable<T> source,
        IReadOnlyList<string> includes,
        QueryableConfig<T>? config = null)
        where T : class
    {
        if (includes.Count == 0)
        {
            return source;
        }

        config ??= QueryableConfig<T>.Default;

        var result = source;
        foreach (var include in includes)
        {
            if (!config.IsIncludeAllowed(include))
            {
                throw new InvalidFieldException(include);
            }

            var canonicalPath = QueryableConfig<T>.ResolveIncludePath(include);
            result = EntityFrameworkQueryableExtensions.Include(result, canonicalPath);
        }

        return result;
    }
}
