using QueryFlow.Abstractions.Enums;

namespace QueryFlow.Validation.Internal;

/// <summary>Derives the set of filter operators that make sense for a given CLR property type.</summary>
internal static class OperatorCatalog
{
    public static IReadOnlySet<FilterOperator> DefaultOperatorsFor(Type propertyType)
    {
        var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var ops = new HashSet<FilterOperator> { FilterOperator.Equals, FilterOperator.NotEquals };

        if (underlying == typeof(string))
        {
            ops.Add(FilterOperator.Contains);
            ops.Add(FilterOperator.StartsWith);
            ops.Add(FilterOperator.EndsWith);
            ops.Add(FilterOperator.In);
            ops.Add(FilterOperator.NotIn);
        }
        else if (underlying.IsEnum)
        {
            ops.Add(FilterOperator.In);
            ops.Add(FilterOperator.NotIn);
        }
        else if (underlying != typeof(bool) && typeof(IComparable).IsAssignableFrom(underlying))
        {
            ops.Add(FilterOperator.GreaterThan);
            ops.Add(FilterOperator.GreaterThanOrEqual);
            ops.Add(FilterOperator.LessThan);
            ops.Add(FilterOperator.LessThanOrEqual);
            ops.Add(FilterOperator.Between);
            ops.Add(FilterOperator.In);
            ops.Add(FilterOperator.NotIn);
        }

        var isNullable = !propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) is not null;
        if (isNullable)
        {
            ops.Add(FilterOperator.IsNull);
            ops.Add(FilterOperator.IsNotNull);
        }

        return ops;
    }
}
