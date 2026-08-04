using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QueryFlow.Abstractions.Exceptions;

namespace QueryFlow.AspNetCore;

/// <summary>
/// Catches <see cref="QueryFlowException"/> (invalid field, invalid operator, page size
/// exceeded, malformed cursor, etc) and turns it into a <c>400 Bad Request</c>
/// <see cref="ProblemDetails"/> response instead of an unhandled <c>500</c>. Register with
/// <see cref="QueryFlowApplicationBuilderExtensions.UseQueryFlowExceptionHandling"/>.
/// </summary>
public sealed class QueryFlowExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (QueryFlowException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Invalid query",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Type = $"https://docs.queryflow.dev/errors/{ex.GetType().Name}"
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}

public static class QueryFlowApplicationBuilderExtensions
{
    /// <summary>Adds <see cref="QueryFlowExceptionMiddleware"/> to the pipeline. Place it early, before endpoint execution.</summary>
    public static IApplicationBuilder UseQueryFlowExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<QueryFlowExceptionMiddleware>();
}
