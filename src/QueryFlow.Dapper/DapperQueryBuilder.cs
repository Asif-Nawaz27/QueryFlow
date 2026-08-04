using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Abstractions.Options;
using QueryFlow.Dapper.Internal;
using QueryFlow.Validation;

namespace QueryFlow.Dapper;

/// <summary>
/// Builds safe, parameterized SQL <c>WHERE</c>/<c>ORDER BY</c>/paging fragments from a
/// <see cref="QueryRequest"/> for use with Dapper's raw-SQL model. Field names are validated
/// against a <see cref="QueryableConfig{T}"/> allowlist and re-checked with a strict identifier
/// pattern before being spliced into SQL text; every value is passed as a bound parameter, never
/// concatenated — this is what makes the output injection-safe despite building SQL as strings.
/// </summary>
public sealed class DapperQueryBuilder<T>
{
    private readonly QueryableConfig<T> _config;
    private readonly IReadOnlyDictionary<string, string> _columnMap;

    public DapperQueryBuilder(QueryableConfig<T>? config = null, IReadOnlyDictionary<string, string>? columnMap = null)
    {
        _config = config ?? QueryableConfig<T>.Default;
        _columnMap = columnMap ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Builds the full set of clause fragments (filter + search + sort + offset paging) for <paramref name="request"/>.</summary>
    public DapperQuery Build(QueryRequest request, DatabaseProvider provider = DatabaseProvider.SqlServer, QueryFlowOptions? options = null)
    {
        options ??= new QueryFlowOptions();
        var allocator = new ParameterAllocator();

        var filterSql = request.FilterGroup is not null
            ? BuildGroupSql(request.FilterGroup, allocator)
            : BuildConditionsSql(request.Filters, allocator);

        var searchSql = BuildSearchSql(request.Search, allocator);
        var whereSql = CombineWhere(filterSql, searchSql);
        var orderBySql = BuildOrderBySql(request.Sort);

        var pageSize = QueryRequestValidator.ClampPageSize(request.PageSize, options);
        var page = Math.Max(1, request.Page ?? 1);
        var pagingSql = BuildPagingSql(page, pageSize, provider, allocator);

        return new DapperQuery(whereSql, orderBySql, pagingSql, allocator.Parameters, page, pageSize);
    }

    /// <summary>Builds just the <c>WHERE</c> clause (filters + search), useful for a standalone <c>COUNT(*)</c> query.</summary>
    public SqlFragment BuildWhere(QueryRequest request)
    {
        var allocator = new ParameterAllocator();
        var filterSql = request.FilterGroup is not null
            ? BuildGroupSql(request.FilterGroup, allocator)
            : BuildConditionsSql(request.Filters, allocator);
        var searchSql = BuildSearchSql(request.Search, allocator);
        return new SqlFragment(CombineWhere(filterSql, searchSql), allocator.Parameters);
    }

    private string ColumnFor(string field) =>
        SqlIdentifier.Validate(_columnMap.TryGetValue(field, out var column) ? column : field);

    private string? BuildConditionsSql(IReadOnlyList<FilterCondition> conditions, ParameterAllocator allocator)
    {
        if (conditions.Count == 0)
        {
            return null;
        }

        var sql = BuildConditionSql(conditions[0], allocator);
        for (var i = 1; i < conditions.Count; i++)
        {
            var connector = conditions[i - 1].Connector == LogicalOperator.And ? " AND " : " OR ";
            sql += connector + BuildConditionSql(conditions[i], allocator);
        }

        return conditions.Count > 1 ? $"({sql})" : sql;
    }

    private string? BuildGroupSql(FilterGroup group, ParameterAllocator allocator)
    {
        var parts = new List<string>();

        var chain = BuildConditionsSql(group.Conditions, allocator);
        if (chain is not null)
        {
            parts.Add(chain);
        }

        foreach (var nested in group.Groups)
        {
            var nestedSql = BuildGroupSql(nested, allocator);
            if (nestedSql is not null)
            {
                parts.Add(nestedSql);
            }
        }

        if (parts.Count == 0)
        {
            return null;
        }

        var separator = group.Operator == LogicalOperator.And ? " AND " : " OR ";
        var combined = string.Join(separator, parts);
        return parts.Count > 1 ? $"({combined})" : combined;
    }

    private string BuildConditionSql(FilterCondition condition, ParameterAllocator allocator)
    {
        condition.EnsureWellFormed();
        if (!_config.IsFilterAllowed(condition.Field, condition.Operator))
        {
            throw _config.IsKnownField(condition.Field)
                ? new InvalidOperatorException(condition.Field, condition.Operator)
                : new InvalidFieldException(condition.Field);
        }

        var column = ColumnFor(condition.Field);

        return condition.Operator switch
        {
            FilterOperator.Equals => $"{column} = {allocator.Add(condition.Value)}",
            FilterOperator.NotEquals => $"{column} <> {allocator.Add(condition.Value)}",
            FilterOperator.GreaterThan => $"{column} > {allocator.Add(condition.Value)}",
            FilterOperator.GreaterThanOrEqual => $"{column} >= {allocator.Add(condition.Value)}",
            FilterOperator.LessThan => $"{column} < {allocator.Add(condition.Value)}",
            FilterOperator.LessThanOrEqual => $"{column} <= {allocator.Add(condition.Value)}",
            FilterOperator.Between => $"{column} BETWEEN {allocator.Add(condition.Values![0])} AND {allocator.Add(condition.Values![1])}",
            FilterOperator.In => $"{column} IN {allocator.Add(condition.Values!.ToList())}",
            FilterOperator.NotIn => $"{column} NOT IN {allocator.Add(condition.Values!.ToList())}",
            FilterOperator.Contains => $"{column} LIKE {allocator.Add($"%{condition.Value}%")}",
            FilterOperator.StartsWith => $"{column} LIKE {allocator.Add($"{condition.Value}%")}",
            FilterOperator.EndsWith => $"{column} LIKE {allocator.Add($"%{condition.Value}")}",
            FilterOperator.IsNull => $"{column} IS NULL",
            FilterOperator.IsNotNull => $"{column} IS NOT NULL",
            _ => throw new InvalidOperatorException(condition.Field, condition.Operator)
        };
    }

    private string? BuildSearchSql(string? term, ParameterAllocator allocator)
    {
        if (string.IsNullOrWhiteSpace(term) || _config.SearchableFields.Count == 0)
        {
            return null;
        }

        var paramToken = allocator.Add($"%{term}%");
        var clauses = _config.SearchableFields.Select(field => $"{ColumnFor(field)} LIKE {paramToken}");
        return $"({string.Join(" OR ", clauses)})";
    }

    private string BuildOrderBySql(IReadOnlyList<SortField> sortFields)
    {
        if (sortFields.Count == 0)
        {
            return string.Empty;
        }

        var clauses = sortFields.Select(sf =>
        {
            if (!_config.IsSortAllowed(sf.Field))
            {
                throw new InvalidFieldException(sf.Field);
            }

            var direction = sf.Direction == SortDirection.Descending ? "DESC" : "ASC";
            return $"{ColumnFor(sf.Field)} {direction}";
        });

        return "ORDER BY " + string.Join(", ", clauses);
    }

    private static string BuildPagingSql(int page, int pageSize, DatabaseProvider provider, ParameterAllocator allocator)
    {
        var offset = (page - 1) * pageSize;
        var offsetToken = allocator.Add(offset);
        var takeToken = allocator.Add(pageSize);

        return provider switch
        {
            DatabaseProvider.SqlServer => $"OFFSET {offsetToken} ROWS FETCH NEXT {takeToken} ROWS ONLY",
            _ => $"LIMIT {takeToken} OFFSET {offsetToken}"
        };
    }

    private static string CombineWhere(string? filterSql, string? searchSql)
    {
        var parts = new[] { filterSql, searchSql }.Where(p => !string.IsNullOrEmpty(p)).ToArray();
        return parts.Length == 0 ? string.Empty : "WHERE " + string.Join(" AND ", parts);
    }
}
