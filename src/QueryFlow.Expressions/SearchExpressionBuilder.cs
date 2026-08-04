using System.Linq.Expressions;
using QueryFlow.Expressions.Caching;
using QueryFlow.Expressions.Internal;

namespace QueryFlow.Expressions;

/// <summary>
/// Builds a case-insensitive, OR-combined <c>Contains</c> predicate across a fixed set of
/// developer-configured searchable string fields (see <c>Searchable()</c> in
/// QueryFlow.Validation), powering the <c>?search=</c> query parameter.
/// </summary>
public static class SearchExpressionBuilder
{
    public static Expression<Func<T, bool>>? Build<T>(string? searchTerm, IReadOnlyList<string> searchableFields)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchableFields.Count == 0)
        {
            return null;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var upperTerm = Expression.Constant(searchTerm.ToUpperInvariant());

        Expression? body = null;
        foreach (var field in searchableFields)
        {
            var propertyChain = PropertyPathCache.Resolve<T>(field);
            var (member, nullGuard) = MemberPathBuilder.Build(parameter, propertyChain);

            if (member.Type != typeof(string))
            {
                continue;
            }

            var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
            var upperMember = Expression.Call(member, ReflectionMethodCache.StringToUpper);
            var contains = Expression.Call(upperMember, ReflectionMethodCache.StringContains, upperTerm);
            var clause = Expression.AndAlso(notNull, contains);
            if (nullGuard is not null)
            {
                clause = Expression.AndAlso(nullGuard, clause);
            }

            body = body is null ? clause : Expression.OrElse(body, clause);
        }

        return body is null ? null : Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
