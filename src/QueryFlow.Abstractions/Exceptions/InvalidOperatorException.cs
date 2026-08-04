using QueryFlow.Abstractions.Enums;

namespace QueryFlow.Abstractions.Exceptions;

/// <summary>
/// Thrown when a filter operator is not valid for the target field's CLR type
/// (e.g. <see cref="FilterOperator.Contains"/> against a numeric column) or is excluded by the
/// configured allowlist for that field.
/// </summary>
public sealed class InvalidOperatorException : QueryFlowException
{
    public string Field { get; }

    public FilterOperator Operator { get; }

    public InvalidOperatorException(string field, FilterOperator @operator)
        : base($"Operator '{@operator}' is not allowed on field '{field}'.")
    {
        Field = field;
        Operator = @operator;
    }
}
