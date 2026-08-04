using System.Collections.Concurrent;
using System.Reflection;

namespace QueryFlow.Core.Internal;

/// <summary>
/// Lets QueryFlow.Core offer real async, non-blocking pagination against EF Core
/// <c>DbSet</c>/<c>IQueryable</c> sources without QueryFlow.Core taking a compile-time
/// dependency on EF Core (keeping the package usable with Dapper or plain in-memory
/// collections). When EF Core is loaded in the process, its
/// <c>EntityFrameworkQueryableExtensions.ToListAsync</c>/<c>CountAsync</c>/<c>LongCountAsync</c>
/// are located once via reflection and invoked directly — genuine async ADO.NET I/O, not a
/// thread-pool hop. When EF Core is not present, callers fall back to the synchronous LINQ
/// operators.
/// </summary>
internal static class EfCoreAsyncBridge
{
    private static readonly Type? ExtensionsType = Type.GetType(
        "Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions, Microsoft.EntityFrameworkCore");

    private static readonly ConcurrentDictionary<Type, MethodInfo?> ToListMethods = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo?> CountMethods = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo?> LongCountMethods = new();

    public static bool IsAvailable => ExtensionsType is not null;

    public static async Task<List<T>> ToListAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
    {
        var method = ResolveGenericMethod<T>("ToListAsync", ToListMethods);
        if (method is not null)
        {
            return await (Task<List<T>>)method.Invoke(null, [source, cancellationToken])!;
        }

        return await Task.Run(source.ToList, cancellationToken);
    }

    public static async Task<int> CountAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
    {
        var method = ResolveGenericMethod<T>("CountAsync", CountMethods);
        if (method is not null)
        {
            return await (Task<int>)method.Invoke(null, [source, cancellationToken])!;
        }

        return await Task.Run(source.Count, cancellationToken);
    }

    public static async Task<long> LongCountAsync<T>(IQueryable<T> source, CancellationToken cancellationToken)
    {
        var method = ResolveGenericMethod<T>("LongCountAsync", LongCountMethods);
        if (method is not null)
        {
            return await (Task<long>)method.Invoke(null, [source, cancellationToken])!;
        }

        return await Task.Run(source.LongCount, cancellationToken);
    }

    private static MethodInfo? ResolveGenericMethod<T>(string name, ConcurrentDictionary<Type, MethodInfo?> cache)
    {
        if (ExtensionsType is null)
        {
            return null;
        }

        return cache.GetOrAdd(typeof(T), static (elementType, state) =>
        {
            var (type, methodName) = state;
            var openMethod = type
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == methodName &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[1].ParameterType == typeof(CancellationToken));

            return openMethod?.MakeGenericMethod(elementType);
        }, (ExtensionsType, name));
    }
}
