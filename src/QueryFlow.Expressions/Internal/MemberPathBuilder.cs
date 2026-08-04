using System.Linq.Expressions;
using System.Reflection;

namespace QueryFlow.Expressions.Internal;

/// <summary>
/// Builds a member-access chain for a resolved property path along with a null-guard for any
/// nullable intermediate segments (e.g. <c>x.Address.City</c> guards against <c>x.Address</c>
/// being null). Needed for predicates evaluated in-memory (LINQ-to-Objects, materialized Dapper
/// rows) — SQL providers like EF Core already propagate NULL correctly on their own, but a
/// compiled delegate walking a null reference throws.
/// </summary>
internal static class MemberPathBuilder
{
    public static (Expression Member, Expression? NullGuard) Build(Expression instance, PropertyInfo[] chain)
    {
        Expression current = instance;
        Expression? guard = null;

        for (var i = 0; i < chain.Length - 1; i++)
        {
            current = Expression.Property(current, chain[i]);
            if (IsNullable(current.Type))
            {
                var notNull = Expression.NotEqual(current, Expression.Constant(null, current.Type));
                guard = guard is null ? notNull : Expression.AndAlso(guard, notNull);
            }
        }

        current = Expression.Property(current, chain[^1]);
        return (current, guard);
    }

    private static bool IsNullable(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}
