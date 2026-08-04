using Swashbuckle.AspNetCore.SwaggerGen;

namespace QueryFlow.OpenApi;

public static class SwaggerGenOptionsExtensions
{
    /// <summary>Registers <see cref="QueryFlowOperationFilter"/> so Swagger/OpenAPI docs show QueryFlow's standard query parameters.</summary>
    public static SwaggerGenOptions AddQueryFlow(this SwaggerGenOptions options)
    {
        options.OperationFilterDescriptors.Add(new FilterDescriptor
        {
            Type = typeof(QueryFlowOperationFilter),
            Arguments = []
        });
        return options;
    }
}
