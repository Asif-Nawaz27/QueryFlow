using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Core.Internal;
using QueryFlow.Validation;

namespace QueryFlow.Core;

/// <summary>Computes Count/Sum/Average/Max/Min aggregates over a query.</summary>
public static class AggregateQueryableExtensions
{
    public static async Task<IReadOnlyList<AggregateResult>> AggregateAsync<T>(
        this IQueryable<T> source,
        IReadOnlyList<AggregateRequest> requests,
        QueryableConfig<T>? config = null,
        CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
        {
            return Array.Empty<AggregateResult>();
        }

        config ??= QueryableConfig<T>.Default;

        var results = new List<AggregateResult>(requests.Count);
        foreach (var request in requests)
        {
            if (request.Field is not null && !config.IsSelectAllowed(request.Field))
            {
                throw new InvalidFieldException(request.Field);
            }

            var value = await AggregateBridge.ComputeAsync(source, request.Function, request.Field, cancellationToken)
                .ConfigureAwait(false);
            results.Add(new AggregateResult(request.Function, request.Field, value));
        }

        return results;
    }
}
