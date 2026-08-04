using System.Collections.Concurrent;
using System.Reflection;

namespace QueryFlow.Expressions.Caching;

/// <summary>
/// Pre-resolved, statically cached <see cref="MethodInfo"/> instances for the framework methods
/// QueryFlow's expression builders splice into filter/search/sort trees. Avoids repeated
/// reflection lookups (<c>typeof(string).GetMethod(...)</c>, generic <c>MakeGenericMethod</c>)
/// on every query.
/// </summary>
internal static class ReflectionMethodCache
{
    public static readonly MethodInfo StringContains =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    public static readonly MethodInfo StringStartsWith =
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;

    public static readonly MethodInfo StringEndsWith =
        typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

    public static readonly MethodInfo StringToUpper =
        typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!;

    public static readonly MethodInfo EnumerableContainsOpen =
        typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);

    private static readonly MethodInfo[] QueryableMethods =
        typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static);

    public static readonly MethodInfo OrderByOpen =
        QueryableMethods.Single(m => m.Name == nameof(Queryable.OrderBy) && m.GetParameters().Length == 2);

    public static readonly MethodInfo OrderByDescendingOpen =
        QueryableMethods.Single(m => m.Name == nameof(Queryable.OrderByDescending) && m.GetParameters().Length == 2);

    public static readonly MethodInfo ThenByOpen =
        QueryableMethods.Single(m => m.Name == nameof(Queryable.ThenBy) && m.GetParameters().Length == 2);

    public static readonly MethodInfo ThenByDescendingOpen =
        QueryableMethods.Single(m => m.Name == nameof(Queryable.ThenByDescending) && m.GetParameters().Length == 2);

    public static readonly MethodInfo SelectOpen =
        QueryableMethods.Single(m =>
            m.Name == nameof(Queryable.Select) &&
            m.GetParameters().Length == 2 &&
            m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2);

    private static readonly ConcurrentGenericMethodCache OrderByCache = new(OrderByOpen);
    private static readonly ConcurrentGenericMethodCache OrderByDescendingCache = new(OrderByDescendingOpen);
    private static readonly ConcurrentGenericMethodCache ThenByCache = new(ThenByOpen);
    private static readonly ConcurrentGenericMethodCache ThenByDescendingCache = new(ThenByDescendingOpen);
    private static readonly ConcurrentGenericMethodCache EnumerableContainsCache = new(EnumerableContainsOpen);
    private static readonly ConcurrentGenericMethodCache SelectCache = new(SelectOpen);

    public static MethodInfo OrderBy(Type entityType, Type keyType) => OrderByCache.Get(entityType, keyType);

    public static MethodInfo OrderByDescending(Type entityType, Type keyType) =>
        OrderByDescendingCache.Get(entityType, keyType);

    public static MethodInfo ThenBy(Type entityType, Type keyType) => ThenByCache.Get(entityType, keyType);

    public static MethodInfo ThenByDescending(Type entityType, Type keyType) =>
        ThenByDescendingCache.Get(entityType, keyType);

    public static MethodInfo EnumerableContains(Type elementType) =>
        EnumerableContainsCache.Get(elementType);

    public static MethodInfo Select(Type sourceType, Type resultType) =>
        SelectCache.Get(sourceType, resultType);

    private sealed class ConcurrentGenericMethodCache(MethodInfo openMethod)
    {
        private readonly ConcurrentDictionary<(Type, Type), MethodInfo> _cache = new();

        public MethodInfo Get(Type type1, Type type2)
        {
            return _cache.GetOrAdd((type1, type2), static (key, m) => m.MakeGenericMethod(key.Item1, key.Item2), openMethod);
        }

        public MethodInfo Get(Type type1)
        {
            return _cache.GetOrAdd((type1, typeof(void)), static (key, m) => m.MakeGenericMethod(key.Item1), openMethod);
        }
    }
}
