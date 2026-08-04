using Microsoft.Extensions.DependencyInjection;
using QueryFlow.Abstractions.Options;

namespace QueryFlow.AspNetCore;

/// <summary>DI registration for QueryFlow.</summary>
public static class QueryFlowServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="QueryFlowOptions"/> (guardrails: max page size, max
    /// filter/sort clause counts, cursor signing key, etc). Optional — every QueryFlow extension
    /// method also accepts an explicit <see cref="QueryFlowOptions"/> and falls back to
    /// framework defaults when neither is supplied.
    /// </summary>
    public static IServiceCollection AddQueryFlow(this IServiceCollection services, Action<QueryFlowOptions>? configure = null)
    {
        var options = new QueryFlowOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        return services;
    }
}
