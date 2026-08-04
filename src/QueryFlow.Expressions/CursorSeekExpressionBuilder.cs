using System.Linq.Expressions;
using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Models;
using QueryFlow.Expressions.Caching;
using QueryFlow.Expressions.Internal;

namespace QueryFlow.Expressions;

/// <summary>
/// Builds keyset ("seek method") predicates for cursor pagination: given the sort fields in
/// effect and the key values of the last row seen, produces
/// <c>(k1 &gt; v1) OR (k1 = v1 AND k2 &gt; v2) OR (k1 = v1 AND k2 = v2 AND k3 &gt; v3) ...</c>
/// (flipped to <c>&lt;</c> per field when that field sorts descending, and reversed entirely
/// when paging backward). This is what makes QueryFlow's cursors stable under concurrent
/// inserts/deletes, unlike offset-based <c>SKIP n</c> paging.
/// </summary>
public static class CursorSeekExpressionBuilder
{
    public static Expression<Func<T, bool>> Build<T>(
        IReadOnlyList<SortField> sortFields,
        IReadOnlyList<object?> lastValues,
        bool backward)
    {
        if (sortFields.Count != lastValues.Count)
        {
            throw new ArgumentException("Sort fields and last values must have the same length.");
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var members = sortFields
            .Select(sf =>
            {
                var chain = PropertyPathCache.Resolve<T>(sf.Field);
                Expression member = parameter;
                foreach (var property in chain)
                {
                    member = Expression.Property(member, property);
                }

                return member;
            })
            .ToArray();

        Expression? orChain = null;
        for (var i = 0; i < sortFields.Count; i++)
        {
            Expression? andChain = null;
            for (var j = 0; j < i; j++)
            {
                var eq = Expression.Equal(members[j], Constant(lastValues[j], members[j].Type));
                andChain = andChain is null ? eq : Expression.AndAlso(andChain, eq);
            }

            var ascending = sortFields[i].Direction == SortDirection.Ascending;
            var strictlyAfter = ascending != backward;

            var comparisonValue = Constant(lastValues[i], members[i].Type);
            var comparison = strictlyAfter
                ? ComparisonBuilder.GreaterThan(members[i], comparisonValue)
                : ComparisonBuilder.LessThan(members[i], comparisonValue);

            andChain = andChain is null ? comparison : Expression.AndAlso(andChain, comparison);
            orChain = orChain is null ? andChain : Expression.OrElse(orChain, andChain);
        }

        return Expression.Lambda<Func<T, bool>>(orChain ?? Expression.Constant(true), parameter);
    }

    private static ConstantExpression Constant(object? value, Type type) =>
        Expression.Constant(ValueConverter.Convert(value, type), type);
}
