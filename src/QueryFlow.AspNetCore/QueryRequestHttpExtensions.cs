using Microsoft.AspNetCore.Http;
using QueryFlow.Abstractions.Models;

namespace QueryFlow.AspNetCore;

/// <summary>Builds a <see cref="QueryRequest"/> from the standard QueryFlow query string parameters.</summary>
public static class QueryRequestHttpExtensions
{
    /// <summary>
    /// Reads <c>filter</c>, <c>sort</c>, <c>search</c>, <c>fields</c>, <c>include</c>, <c>page</c>,
    /// <c>pageSize</c> and <c>cursor</c> off the request's query string and builds a <see cref="QueryRequest"/>.
    /// </summary>
    public static QueryRequest ToQueryRequest(this HttpRequest request)
    {
        var query = request.Query;

        var filters = FilterQueryStringParser.Parse(query["filter"]);
        var sort = ParseList(query["sort"]).Select(SortField.Parse).ToArray();
        var fields = ParseList(query["fields"]);
        var includes = ParseList(query["include"]);
        var search = query["search"].Count > 0 ? query["search"].ToString() : null;
        var cursor = query["cursor"].Count > 0 ? query["cursor"].ToString() : null;

        int? page = int.TryParse(query["page"], out var p) ? p : null;
        int? pageSize = int.TryParse(query["pageSize"], out var ps) ? ps : null;

        return new QueryRequest
        {
            Filters = filters,
            Sort = sort,
            Fields = fields,
            Includes = includes,
            Search = search,
            Cursor = cursor,
            Page = page,
            PageSize = pageSize
        };
    }

    private static string[] ParseList(Microsoft.Extensions.Primitives.StringValues value)
    {
        if (value.Count == 0)
        {
            return [];
        }

        return value
            .SelectMany(v => (v ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();
    }
}
