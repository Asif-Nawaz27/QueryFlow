namespace QueryFlow.Abstractions.Enums;

/// <summary>
/// The comparison operator applied to a single filter condition.
/// </summary>
public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Between,
    In,
    NotIn,
    Contains,
    StartsWith,
    EndsWith,
    IsNull,
    IsNotNull
}
