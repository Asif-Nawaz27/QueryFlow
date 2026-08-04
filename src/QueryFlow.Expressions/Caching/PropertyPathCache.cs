using System.Collections.Concurrent;
using System.Reflection;
using QueryFlow.Abstractions.Exceptions;

namespace QueryFlow.Expressions.Caching;

/// <summary>
/// Resolves dotted field paths (e.g. <c>"Address.City"</c>) against a CLR type via reflection
/// and caches the resulting <see cref="PropertyInfo"/> chain, so repeated queries against the
/// same type never pay reflection lookup cost twice. Matching is case-insensitive.
/// </summary>
public static class PropertyPathCache
{
    private static readonly ConcurrentDictionary<(Type Type, string Path), PropertyInfo[]?> Cache = new();

    /// <summary>
    /// Resolves <paramref name="path"/> against <paramref name="type"/>, returning the chain of
    /// properties to traverse, or null if any segment does not exist.
    /// </summary>
    public static PropertyInfo[]? TryResolve(Type type, string path)
    {
        var key = (type, path);
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            Cache[key] = null;
            return null;
        }

        var chain = new PropertyInfo[segments.Length];
        var currentType = type;
        for (var i = 0; i < segments.Length; i++)
        {
            var property = currentType.GetProperty(
                segments[i],
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property is null)
            {
                Cache[key] = null;
                return null;
            }

            chain[i] = property;
            currentType = property.PropertyType;
        }

        Cache[key] = chain;
        return chain;
    }

    /// <summary>Resolves <paramref name="path"/> against <typeparamref name="T"/>, throwing if not found.</summary>
    public static PropertyInfo[] Resolve<T>(string path) => Resolve(typeof(T), path);

    /// <summary>Resolves <paramref name="path"/> against <paramref name="type"/>, throwing if not found.</summary>
    public static PropertyInfo[] Resolve(Type type, string path) =>
        TryResolve(type, path) ?? throw new InvalidFieldException(path);
}
