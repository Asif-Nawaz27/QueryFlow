using Microsoft.AspNetCore.Http;
using QueryFlow.Abstractions.Models;

namespace QueryFlow.AspNetCore;

/// <summary>
/// Minimal API parameter type that auto-binds a <see cref="QueryRequest"/> from the query
/// string, so a handler can just declare a <see cref="QueryRequestParameter"/> parameter:
/// <c>app.MapGet("/users", (QueryRequestParameter q, AppDbContext db) => ...)</c>. Implicitly
/// converts to <see cref="QueryRequest"/> for direct use.
/// </summary>
public sealed class QueryRequestParameter
{
    public QueryRequest Value { get; }

    public QueryRequestParameter(QueryRequest value) => Value = value;

    public static ValueTask<QueryRequestParameter?> BindAsync(HttpContext context) =>
        ValueTask.FromResult<QueryRequestParameter?>(new QueryRequestParameter(context.Request.ToQueryRequest()));

    public static implicit operator QueryRequest(QueryRequestParameter parameter) => parameter.Value;
}
