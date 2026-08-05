using Microsoft.OpenApi;
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
    private static readonly (string Name, string Description, JsonSchemaType Type)[] StandardParameters =
    [
        ("filter", "Filter expression, e.g. `age gt 18 and city eq \"NY\"`.", JsonSchemaType.String),
        ("sort", "Comma-separated sort fields; prefix with `-` for descending, e.g. `LastName,-CreatedDate`.", JsonSchemaType.String),
        ("search", "Free-text search term, matched against the endpoint's configured searchable fields.", JsonSchemaType.String),
        ("fields", "Comma-separated field selection, e.g. `id,name,email`. Omit to return all fields.", JsonSchemaType.String),
        ("include", "Comma-separated navigation properties to eagerly load, e.g. `customer,address`.", JsonSchemaType.String),
        ("page", "1-based page number for offset pagination.", JsonSchemaType.Integer),
        ("pageSize", "Number of items per page.", JsonSchemaType.Integer),
        ("cursor", "Opaque cursor token for cursor-based pagination; overrides `page` when present.", JsonSchemaType.String)
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
