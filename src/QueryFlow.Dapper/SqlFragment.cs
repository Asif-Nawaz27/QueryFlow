using global::Dapper;

namespace QueryFlow.Dapper;

/// <summary>A SQL text fragment (e.g. a <c>WHERE</c> clause) paired with its parameters.</summary>
public sealed record SqlFragment(string Sql, DynamicParameters Parameters)
{
    public static SqlFragment Empty { get; } = new(string.Empty, new DynamicParameters());
}
