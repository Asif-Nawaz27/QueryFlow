using System.Linq.Expressions;
using System.Reflection;
using QueryFlow.Abstractions.Enums;
using QueryFlow.Expressions;

namespace QueryFlow.Core.Internal;

/// <summary>
/// Resolves and invokes the correct <see cref="Queryable"/> Sum/Average/Max/Min overload (or,
/// when EF Core is loaded, its async equivalents) for a runtime-known field type, mirroring
/// <see cref="EfCoreAsyncBridge"/>'s soft-dependency approach so QueryFlow.Core stays
/// ORM-independent while still getting real async execution against EF Core.
/// </summary>
internal static class AggregateBridge
{
    private static readonly MethodInfo[] QueryableMethods =
        typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static);

    private static readonly Type? EfExtensionsType = Type.GetType(
        "Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions, Microsoft.EntityFrameworkCore");

    private static readonly MethodInfo[] EfMethods =
        EfExtensionsType?.GetMethods(BindingFlags.Public | BindingFlags.Static) ?? [];

    public static async Task<object?> ComputeAsync<T>(
        IQueryable<T> source,
        AggregateFunction function,
        string? field,
        CancellationToken cancellationToken)
    {
        if (function == AggregateFunction.Count)
        {
            return await EfCoreAsyncBridge.LongCountAsync(source, cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(field))
        {
            throw new ArgumentException($"Aggregate function '{function}' requires a field.", nameof(field));
        }

        var selector = AggregateExpressionBuilder.BuildSelector<T>(field);
        var resultType = selector.Body.Type;
        var methodName = function.ToString();

        var efMethod = FindMethod(EfMethods, methodName + "Async", typeof(T), resultType);
        if (efMethod is not null)
        {
            var task = (Task)efMethod.Invoke(null, [source, selector, cancellationToken])!;
            await task.ConfigureAwait(false);
            return task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
        }

        var syncMethod = FindMethod(QueryableMethods, methodName, typeof(T), resultType)
                          ?? throw new InvalidOperationException(
                              $"No '{methodName}' overload is available for field '{field}' of type '{resultType.Name}'.");

        return await Task.Run(() => syncMethod.Invoke(null, [source, selector]), cancellationToken).ConfigureAwait(false);
    }

    private static MethodInfo? FindMethod(IEnumerable<MethodInfo> candidates, string name, Type sourceType, Type resultType)
    {
        foreach (var method in candidates)
        {
            if (method.Name != name)
            {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length is not (2 or 3))
            {
                continue;
            }

            if (parameters.Length == 3 && parameters[2].ParameterType != typeof(CancellationToken))
            {
                continue;
            }

            var selectorParamType = parameters[1].ParameterType;
            if (!selectorParamType.IsGenericType)
            {
                continue;
            }

            var innerFuncType = selectorParamType.GetGenericArguments().ElementAtOrDefault(0);
            if (innerFuncType is not { IsGenericType: true })
            {
                continue;
            }

            var funcArgs = innerFuncType.GetGenericArguments();
            if (funcArgs.Length != 2)
            {
                continue;
            }

            var genericParams = method.GetGenericArguments();
            try
            {
                if (genericParams.Length == 1)
                {
                    if (funcArgs[1] != resultType)
                    {
                        continue;
                    }

                    return method.MakeGenericMethod(sourceType);
                }

                if (genericParams.Length == 2)
                {
                    return method.MakeGenericMethod(sourceType, resultType);
                }
            }
            catch (ArgumentException)
            {
                // Constraint mismatch (e.g. Max/Min on a non-comparable projected type) — try the next candidate.
            }
        }

        return null;
    }
}
