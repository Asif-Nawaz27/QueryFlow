using QueryFlow.Abstractions.Enums;

namespace QueryFlow.Abstractions.Models;

/// <summary>
/// A single filter condition applied to one field, e.g. <c>Age GreaterThan 18</c>.
/// </summary>
/// <param name="Field">The logical field name as exposed to clients (case-insensitive, mapped via an allowlist).</param>
/// <param name="Operator">The comparison operator to apply.</param>
/// <param name="Value">The comparison value for single-value operators.</param>
/// <param name="Values">The comparison values for the Between, In and NotIn operators.</param>
/// <param name="Connector">How this condition combines with the next sibling condition in the same list. Ignored for the last condition.</param>
public sealed record FilterCondition(
    string Field,
    FilterOperator Operator,
    object? Value = null,
    IReadOnlyList<object?>? Values = null,
    LogicalOperator Connector = LogicalOperator.And)
{
    /// <summary>
    /// Validates the shape of the condition (e.g. that the Between operator has exactly two values).
    /// Throws <see cref="ArgumentException"/> if malformed. Does not validate the field name or operator allowlist.
    /// </summary>
    public void EnsureWellFormed()
    {
        if (string.IsNullOrWhiteSpace(Field))
        {
            throw new ArgumentException("Filter condition must specify a field.", nameof(Field));
        }

        switch (Operator)
        {
            case FilterOperator.Between when Values is null || Values.Count != 2:
                throw new ArgumentException($"Operator '{Operator}' on field '{Field}' requires exactly two values.");
            case FilterOperator.In or FilterOperator.NotIn when Values is null || Values.Count == 0:
                throw new ArgumentException($"Operator '{Operator}' on field '{Field}' requires at least one value.");
            case FilterOperator.IsNull or FilterOperator.IsNotNull:
                break;
            default:
                if (Operator is not (FilterOperator.Between or FilterOperator.In or FilterOperator.NotIn)
                    && Value is null)
                {
                    throw new ArgumentException($"Operator '{Operator}' on field '{Field}' requires a value.");
                }

                break;
        }
    }
}
