using System.Linq.Expressions;
using System.Reflection;

namespace QueryFlow.Expressions.Internal;

/// <summary>
/// Builds relational comparisons (&gt;, &gt;=, &lt;, &lt;=). Strings have no relational operator
/// overloads in .NET, so string comparisons are rewritten as <c>string.Compare(a, b) op 0</c>,
/// which EF Core and other providers translate to a SQL string comparison; every other type uses
/// the native binary operator.
/// </summary>
internal static class ComparisonBuilder
{
    private static readonly MethodInfo StringCompare =
        typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;

    public static Expression GreaterThan(Expression left, Expression right) => Build(left, right, ExpressionType.GreaterThan);

    public static Expression GreaterThanOrEqual(Expression left, Expression right) => Build(left, right, ExpressionType.GreaterThanOrEqual);

    public static Expression LessThan(Expression left, Expression right) => Build(left, right, ExpressionType.LessThan);

    public static Expression LessThanOrEqual(Expression left, Expression right) => Build(left, right, ExpressionType.LessThanOrEqual);

    private static Expression Build(Expression left, Expression right, ExpressionType type)
    {
        if (left.Type == typeof(string))
        {
            var call = Expression.Call(StringCompare, left, right);
            var zero = Expression.Constant(0);
            return type switch
            {
                ExpressionType.GreaterThan => Expression.GreaterThan(call, zero),
                ExpressionType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(call, zero),
                ExpressionType.LessThan => Expression.LessThan(call, zero),
                ExpressionType.LessThanOrEqual => Expression.LessThanOrEqual(call, zero),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        return type switch
        {
            ExpressionType.GreaterThan => Expression.GreaterThan(left, right),
            ExpressionType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
            ExpressionType.LessThan => Expression.LessThan(left, right),
            ExpressionType.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
