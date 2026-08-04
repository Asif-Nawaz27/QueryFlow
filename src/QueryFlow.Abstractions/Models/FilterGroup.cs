using QueryFlow.Abstractions.Enums;

namespace QueryFlow.Abstractions.Models;

/// <summary>
/// A composite group of filter conditions and nested groups, combined with a single
/// <see cref="LogicalOperator"/>. Enables arbitrarily nested boolean expressions,
/// e.g. <c>(Age gt 18 AND Country eq "US") OR IsVip eq true</c>.
/// </summary>
public sealed record FilterGroup(
    LogicalOperator Operator,
    IReadOnlyList<FilterCondition>? Conditions = null,
    IReadOnlyList<FilterGroup>? Groups = null)
{
    public static readonly IReadOnlyList<FilterCondition> NoConditions = Array.Empty<FilterCondition>();
    public static readonly IReadOnlyList<FilterGroup> NoGroups = Array.Empty<FilterGroup>();

    public IReadOnlyList<FilterCondition> Conditions { get; init; } = Conditions ?? NoConditions;

    public IReadOnlyList<FilterGroup> Groups { get; init; } = Groups ?? NoGroups;

    public bool IsEmpty => Conditions.Count == 0 && Groups.Count == 0;

    public static FilterGroup And(params FilterCondition[] conditions) =>
        new(LogicalOperator.And, conditions);

    public static FilterGroup Or(params FilterCondition[] conditions) =>
        new(LogicalOperator.Or, conditions);
}
