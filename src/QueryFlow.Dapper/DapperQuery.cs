using global::Dapper;

namespace QueryFlow.Dapper;

/// <summary>
/// The SQL clause fragments produced by <see cref="DapperQueryBuilder{T}"/>, ready to splice into
/// a hand-written SQL statement: <c>$"SELECT * FROM Books {query.WhereSql} {query.OrderBySql} {query.PagingSql}"</c>
/// and, for the total count, <c>$"SELECT COUNT(*) FROM Books {query.WhereSql}"</c> reusing the
/// same <see cref="Parameters"/>.
/// </summary>
public sealed record DapperQuery(
    string WhereSql,
    string OrderBySql,
    string PagingSql,
    DynamicParameters Parameters,
    int Page,
    int PageSize);
