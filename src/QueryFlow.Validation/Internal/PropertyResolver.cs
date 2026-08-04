using System.Collections.Concurrent;
using System.Reflection;
using QueryFlow.Abstractions.Exceptions;

namespace QueryFlow.Validation.Internal;

/// <summary>Lightweight, cached dotted-path property type resolution used for allowlist configuration and validation.</summary>
internal static class PropertyResolver
{
    private static readonly ConcurrentDictionary<(Type, string), Type?> Cache = new();

    public static Type? TryResolveType<T>(string path) => TryResolveType(typeof(T), path);

    public static Type? TryResolveType(Type type, string path)
    {
        var key = (type, path);
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var currentType = type;
        foreach (var segment in segments)
        {
            var property = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
            {
                Cache[key] = null;
                return null;
            }

            currentType = property.PropertyType;
        }

        Cache[key] = currentType;
        return currentType;
    }

    public static Type ResolveType<T>(string path) =>
        TryResolveType<T>(path) ?? throw new InvalidFieldException(path);

    public static void EnsureExists<T>(string path)
    {
        if (TryResolveType<T>(path) is null)
        {
            throw new InvalidFieldException(path);
        }
    }
}
