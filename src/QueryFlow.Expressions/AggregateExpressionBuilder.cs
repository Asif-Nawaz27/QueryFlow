using System.Linq.Expressions;
using QueryFlow.Expressions.Caching;

namespace QueryFlow.Expressions;

/// <summary>Builds the field-access selector expression used by Sum/Average/Max/Min aggregates.</summary>
public static class AggregateExpressionBuilder
{
    public static LambdaExpression BuildSelector<T>(string field)
    {
        var propertyChain = PropertyPathCache.Resolve<T>(field);
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression member = parameter;
        foreach (var property in propertyChain)
        {
            member = Expression.Property(member, property);
        }

        return Expression.Lambda(member, parameter);
    }
}
