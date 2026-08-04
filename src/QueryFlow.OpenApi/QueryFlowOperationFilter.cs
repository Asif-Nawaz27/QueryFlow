using Microsoft.OpenApi.Models;
using QueryFlow.Abstractions.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QueryFlow.OpenApi;

/// <summary>
/// Documents the standard <c>filter</c>/<c>sort</c>/<c>search</c>/<c>fields</c>/<c>include</c>/
/// <c>page</c>/<c>pageSize</c>/<c>cursor</c> query parameters on any endpoint that binds a
/// <see cref="QueryRequest"/> or <c>QueryFlow.AspNetCore.QueryRequestParameter</c>, since those
/// bind via a custom <c>BindAsync</c> convention that Swagger/Swashbuckle cannot introspect on
/// its own.
/// </summary>
public sealed class QueryFlowOperationFilter : IOperationFilter
{
    private static readonly (string Name, string Description, string Type)[] StandardParameters =
    [
        ("filter", "Filter expression, e.g. `age gt 18 and city eq \"NY\"`.", "string"),
        ("sort", "Comma-separated sort fields; prefix with `-` for descending, e.g. `LastName,-CreatedDate`.", "string"),
        ("search", "Free-text search term, matched against the endpoint's configured searchable fields.", "string"),
        ("fields", "Comma-separated field selection, e.g. `id,name,email`. Omit to return all fields.", "string"),
        ("include", "Comma-separated navigation properties to eagerly load, e.g. `customer,address`.", "string"),
        ("page", "1-based page number for offset pagination.", "integer"),
        ("pageSize", "Number of items per page.", "integer"),
        ("cursor", "Opaque cursor token for cursor-based pagination; overrides `page` when present.", "string")
    ];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var bindsQueryRequest = context.ApiDescription.ParameterDescriptions.Any(p =>
            p.Type.Name is nameof(QueryRequest) or "QueryRequestParameter");

        if (!bindsQueryRequest)
        {
            return;
        }

        operation.Parameters ??= [];

        foreach (var (name, description, type) in StandardParameters)
        {
            if (operation.Parameters.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = name,
                In = ParameterLocation.Query,
                Description = description,
                Required = false,
                Schema = new OpenApiSchema { Type = type }
            });
        }
    }
}
