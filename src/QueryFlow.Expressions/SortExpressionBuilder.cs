using System.Linq.Expressions;
using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Models;
using QueryFlow.Expressions.Caching;

namespace QueryFlow.Expressions;

/// <summary>
/// Applies one or more <see cref="SortField"/> entries to an <see cref="IQueryable{T}"/> using
/// <c>OrderBy</c>/<c>ThenBy</c> chains built via cached generic method definitions, so ordering
/// on arbitrary, runtime-known key types stays translatable by LINQ providers.
/// </summary>
public static class SortExpressionBuilder
{
    public static IQueryable<T> Apply<T>(IQueryable<T> source, IReadOnlyList<SortField> sortFields)
    {
        if (sortFields.Count == 0)
        {
            return source;
        }

        IOrderedQueryable<T>? ordered = null;

        foreach (var sortField in sortFields)
        {
            var propertyChain = PropertyPathCache.Resolve<T>(sortField.Field);
            var parameter = Expression.Parameter(typeof(T), "x");
            Expression member = parameter;
            foreach (var property in propertyChain)
            {
                member = Expression.Property(member, property);
            }

            var keySelector = Expression.Lambda(member, parameter);
            var isDescending = sortField.Direction == SortDirection.Descending;

            var method = ordered is null
                ? (isDescending ? ReflectionMethodCache.OrderByDescending(typeof(T), member.Type) : ReflectionMethodCache.OrderBy(typeof(T), member.Type))
                : (isDescending ? ReflectionMethodCache.ThenByDescending(typeof(T), member.Type) : ReflectionMethodCache.ThenBy(typeof(T), member.Type));

            var source0 = ordered is null ? (IQueryable)source : ordered;
            ordered = (IOrderedQueryable<T>)method.Invoke(null, [source0, keySelector])!;
        }

        return ordered ?? source;
    }
}
