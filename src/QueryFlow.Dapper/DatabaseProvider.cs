namespace QueryFlow.Dapper;

/// <summary>Selects the offset-paging SQL dialect to emit (LIMIT/OFFSET syntax differs by engine).</summary>
public enum DatabaseProvider
{
    SqlServer,
    PostgreSql,
    MySql,
    Sqlite
}
