using QueryFlow.Abstractions.Enums;

namespace QueryFlow.Abstractions.Models;

/// <summary>
/// A request to compute an aggregate function over a field (ignored for <see cref="AggregateFunction.Count"/>,
/// which applies to the whole result set).
/// </summary>
public sealed record AggregateRequest(AggregateFunction Function, string? Field = null);

/// <summary>
/// The computed result of a single <see cref="AggregateRequest"/>.
/// </summary>
public sealed record AggregateResult(AggregateFunction Function, string? Field, object? Value);
