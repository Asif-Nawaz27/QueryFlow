using System.Linq.Expressions;
using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Expressions.Caching;
using QueryFlow.Expressions.Internal;

namespace QueryFlow.Expressions;

/// <summary>
/// Builds provider-translatable <see cref="Expression{TDelegate}"/> predicates from
/// <see cref="FilterCondition"/> / <see cref="FilterGroup"/> request models. Property and method
/// reflection lookups are cached (see <see cref="PropertyPathCache"/> and
/// <see cref="ReflectionMethodCache"/>); the expression tree itself is rebuilt per call so it
/// stays translatable by LINQ providers such as EF Core.
/// </summary>
public static class FilterExpressionBuilder
{
    /// <summary>Builds a predicate combining <paramref name="conditions"/> left-to-right using each condition's <see cref="FilterCondition.Connector"/>.</summary>
    public static Expression<Func<T, bool>> Build<T>(IReadOnlyList<FilterCondition> conditions)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var body = BuildConditionChain<T>(parameter, conditions);
        return Expression.Lambda<Func<T, bool>>(body ?? Expression.Constant(true), parameter);
    }

    /// <summary>Builds a predicate from a nested <see cref="FilterGroup"/> tree.</summary>
    public static Expression<Func<T, bool>> Build<T>(FilterGroup group)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var body = BuildGroup<T>(parameter, group);
        return Expression.Lambda<Func<T, bool>>(body ?? Expression.Constant(true), parameter);
    }

    private static Expression? BuildConditionChain<T>(ParameterExpression parameter, IReadOnlyList<FilterCondition> conditions)
    {
        if (conditions.Count == 0)
        {
            return null;
        }

        Expression result = BuildCondition<T>(parameter, conditions[0]);
        for (var i = 1; i < conditions.Count; i++)
        {
            var next = BuildCondition<T>(parameter, conditions[i]);
            var connector = conditions[i - 1].Connector;
            result = connector == LogicalOperator.And
                ? Expression.AndAlso(result, next)
                : Expression.OrElse(result, next);
        }

        return result;
    }

    private static Expression? BuildGroup<T>(ParameterExpression parameter, FilterGroup group)
    {
        var parts = new List<Expression>();

        var chain = BuildConditionChain<T>(parameter, group.Conditions);
        if (chain is not null)
        {
            parts.Add(chain);
        }

        foreach (var nested in group.Groups)
        {
            var nestedExpr = BuildGroup<T>(parameter, nested);
            if (nestedExpr is not null)
            {
                parts.Add(nestedExpr);
            }
        }

        if (parts.Count == 0)
        {
            return null;
        }

        var combine = group.Operator == LogicalOperator.And
            ? (Func<Expression, Expression, Expression>)Expression.AndAlso
            : Expression.OrElse;

        return parts.Aggregate(combine);
    }

    /// <summary>Builds a single boolean-valued expression for one condition against <paramref name="parameter"/>.</summary>
    public static Expression BuildCondition<T>(ParameterExpression parameter, FilterCondition condition)
    {
        condition.EnsureWellFormed();

        var propertyChain = PropertyPathCache.Resolve<T>(condition.Field);
        var (member, nullGuard) = MemberPathBuilder.Build(parameter, propertyChain);
        var propertyType = member.Type;

        Expression baseExpression = condition.Operator switch
        {
            FilterOperator.Equals => Expression.Equal(member, Constant(condition.Value, propertyType)),
            FilterOperator.NotEquals => Expression.NotEqual(member, Constant(condition.Value, propertyType)),
            FilterOperator.GreaterThan => ComparisonBuilder.GreaterThan(member, Constant(condition.Value, propertyType)),
            FilterOperator.GreaterThanOrEqual => ComparisonBuilder.GreaterThanOrEqual(member, Constant(condition.Value, propertyType)),
            FilterOperator.LessThan => ComparisonBuilder.LessThan(member, Constant(condition.Value, propertyType)),
            FilterOperator.LessThanOrEqual => ComparisonBuilder.LessThanOrEqual(member, Constant(condition.Value, propertyType)),
            FilterOperator.Between => BuildBetween(member, condition, propertyType),
            FilterOperator.In => BuildIn(member, condition, propertyType, negate: false),
            FilterOperator.NotIn => BuildIn(member, condition, propertyType, negate: true),
            FilterOperator.Contains => BuildStringCall(member, condition, propertyType, ReflectionMethodCache.StringContains),
            FilterOperator.StartsWith => BuildStringCall(member, condition, propertyType, ReflectionMethodCache.StringStartsWith),
            FilterOperator.EndsWith => BuildStringCall(member, condition, propertyType, ReflectionMethodCache.StringEndsWith),
            FilterOperator.IsNull => BuildIsNull(member, propertyType, condition.Field, negate: false),
            FilterOperator.IsNotNull => BuildIsNull(member, propertyType, condition.Field, negate: true),
            _ => throw new InvalidOperatorException(condition.Field, condition.Operator)
        };

        if (nullGuard is null)
        {
            return baseExpression;
        }

        // A null intermediate navigation (e.g. x.Address is null for "Address.City") means the
        // leaf is effectively null too: IsNull should still match; every other operator should not.
        return condition.Operator == FilterOperator.IsNull
            ? Expression.OrElse(Expression.Not(nullGuard), baseExpression)
            : Expression.AndAlso(nullGuard, baseExpression);
    }

    private static Expression BuildBetween(Expression member, FilterCondition condition, Type propertyType)
    {
        var low = Constant(condition.Values![0], propertyType);
        var high = Constant(condition.Values![1], propertyType);
        return Expression.AndAlso(
            ComparisonBuilder.GreaterThanOrEqual(member, low),
            ComparisonBuilder.LessThanOrEqual(member, high));
    }

    private static Expression BuildIn(Expression member, FilterCondition condition, Type propertyType, bool negate)
    {
        var elementType = propertyType;
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
        foreach (var raw in condition.Values!)
        {
            list.Add(ValueConverter.Convert(raw, elementType));
        }

        var constant = Expression.Constant(list, listType);
        var containsMethod = ReflectionMethodCache.EnumerableContains(elementType);
        var call = Expression.Call(containsMethod, constant, member);
        return negate ? Expression.Not(call) : call;
    }

    private static Expression BuildStringCall(Expression member, FilterCondition condition, Type propertyType, System.Reflection.MethodInfo method)
    {
        if (propertyType != typeof(string))
        {
            throw new InvalidOperatorException(condition.Field, condition.Operator);
        }

        var upperMember = Expression.Call(member, ReflectionMethodCache.StringToUpper);
        var value = ((string)ValueConverter.Convert(condition.Value, typeof(string))!).ToUpperInvariant();
        return Expression.AndAlso(
            Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
            Expression.Call(upperMember, method, Expression.Constant(value)));
    }

    private static Expression BuildIsNull(Expression member, Type propertyType, string field, bool negate)
    {
        var isNullable = !propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) is not null;
        if (!isNullable)
        {
            throw new InvalidOperatorException(field, negate ? FilterOperator.IsNotNull : FilterOperator.IsNull);
        }

        var nullConstant = Expression.Constant(null, propertyType);
        return negate ? Expression.NotEqual(member, nullConstant) : Expression.Equal(member, nullConstant);
    }

    private static ConstantExpression Constant(object? value, Type propertyType)
    {
        var converted = ValueConverter.Convert(value, propertyType);
        return Expression.Constant(converted, propertyType);
    }
}
