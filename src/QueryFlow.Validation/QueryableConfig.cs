using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using QueryFlow.Abstractions.Enums;
using QueryFlow.Validation.Internal;

namespace QueryFlow.Validation;

/// <summary>
/// Declares which fields of <typeparamref name="T"/> may be filtered, sorted, searched,
/// selected and included, and which operators are permitted per field. This is the allowlist
/// that guards against over-fetching and property/operator injection.
/// </summary>
/// <remarks>
/// Zero configuration is required to get started: an unconfigured <see cref="QueryableConfig{T}"/>
/// permits filtering/sorting/selecting on every public, top-level property of <typeparamref name="T"/>
/// with all operators valid for each property's CLR type. Calling any <c>AllowFilter</c>/<c>AllowSort</c>/
/// <c>Searchable</c>/<c>AllowSelect</c> overload switches that specific capability into strict
/// allowlist mode — only explicitly registered fields are then permitted for that capability.
/// </remarks>
public sealed class QueryableConfig<T>
{
    private readonly Dictionary<string, IReadOnlySet<FilterOperator>> _filterFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _sortFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _searchFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _selectFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _includeFields = new(StringComparer.OrdinalIgnoreCase);

    private bool _filterAllowlistActive;
    private bool _sortAllowlistActive;
    private bool _searchAllowlistActive;
    private bool _selectAllowlistActive;
    private bool _includeAllowlistActive;

    private static readonly ConcurrentDictionary<string, IReadOnlySet<FilterOperator>> DefaultOperatorCache = new();

    /// <summary>A wide-open configuration: every public top-level property is filterable, sortable and selectable.</summary>
    public static QueryableConfig<T> Default { get; } = new();

    public QueryableConfig<T> AllowFilter(Expression<Func<T, object?>> property, params FilterOperator[] operators) =>
        AllowFilter(MemberNameExtractor.GetPath(property), operators);

    public QueryableConfig<T> AllowFilter(string field, params FilterOperator[] operators)
    {
        _filterAllowlistActive = true;
        var propertyType = Internal.PropertyResolver.ResolveType<T>(field);
        var allowedOps = operators.Length > 0
            ? new HashSet<FilterOperator>(operators)
            : DefaultOperatorCache.GetOrAdd(propertyType.FullName ?? propertyType.Name, _ => Internal.OperatorCatalog.DefaultOperatorsFor(propertyType));

        _filterFields[field] = allowedOps;
        return this;
    }

    public QueryableConfig<T> AllowSort(Expression<Func<T, object?>> property) =>
        AllowSort(MemberNameExtractor.GetPath(property));

    public QueryableConfig<T> AllowSort(string field)
    {
        _sortAllowlistActive = true;
        Internal.PropertyResolver.EnsureExists<T>(field);
        _sortFields.Add(field);
        return this;
    }

    public QueryableConfig<T> Searchable(Expression<Func<T, string?>> property) =>
        Searchable(MemberNameExtractor.GetPath(property));

    public QueryableConfig<T> Searchable(string field)
    {
        _searchAllowlistActive = true;
        var propertyType = Internal.PropertyResolver.ResolveType<T>(field);
        if (propertyType != typeof(string))
        {
            throw new ArgumentException($"Field '{field}' must be a string property to be searchable.", nameof(field));
        }

        _searchFields.Add(field);
        return this;
    }

    public QueryableConfig<T> AllowSelect(Expression<Func<T, object?>> property) =>
        AllowSelect(MemberNameExtractor.GetPath(property));

    public QueryableConfig<T> AllowSelect(string field)
    {
        _selectAllowlistActive = true;
        Internal.PropertyResolver.EnsureExists<T>(field);
        _selectFields.Add(field);
        return this;
    }

    public QueryableConfig<T> AllowInclude(string navigationProperty)
    {
        _includeAllowlistActive = true;
        _includeFields.Add(navigationProperty);
        return this;
    }

    /// <summary>Registers a field as filterable, sortable and selectable in one call.</summary>
    public QueryableConfig<T> Allow(Expression<Func<T, object?>> property, params FilterOperator[] operators)
    {
        var field = MemberNameExtractor.GetPath(property);
        AllowFilter(field, operators);
        AllowSort(field);
        AllowSelect(field);
        return this;
    }

    /// <summary>True when <paramref name="field"/> resolves to a real property on <typeparamref name="T"/>, regardless of whether it is allowlisted for any specific capability.</summary>
    public bool IsKnownField(string field) => Internal.PropertyResolver.TryResolveType<T>(field) is not null;

    public bool IsFilterAllowed(string field, FilterOperator @operator)
    {
        if (!_filterAllowlistActive)
        {
            var propertyType = Internal.PropertyResolver.TryResolveType<T>(field);
            if (propertyType is null)
            {
                return false;
            }

            var defaults = DefaultOperatorCache.GetOrAdd(propertyType.FullName ?? propertyType.Name, _ => Internal.OperatorCatalog.DefaultOperatorsFor(propertyType));
            return defaults.Contains(@operator);
        }

        return _filterFields.TryGetValue(field, out var ops) && ops.Contains(@operator);
    }

    public bool IsSortAllowed(string field) =>
        !_sortAllowlistActive
            ? Internal.PropertyResolver.TryResolveType<T>(field) is not null
            : _sortFields.Contains(field);

    public bool IsSearchable(string field) =>
        _searchAllowlistActive && _searchFields.Contains(field);

    public IReadOnlyCollection<string> SearchableFields => _searchFields;

    public bool IsSelectAllowed(string field) =>
        !_selectAllowlistActive
            ? Internal.PropertyResolver.TryResolveType<T>(field) is not null
            : _selectFields.Contains(field);

    public bool IsIncludeAllowed(string navigationProperty) =>
        !_includeAllowlistActive
            ? typeof(T).GetProperty(navigationProperty, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is not null
            : _includeFields.Contains(navigationProperty);

    /// <summary>
    /// Resolves <paramref name="navigationPath"/> (matched case-insensitively, e.g. from a query
    /// string) to the exact-case navigation path EF Core's string-based <c>Include</c> requires.
    /// Supports dotted paths like <c>"orders.items"</c> -&gt; <c>"Orders.Items"</c>.
    /// </summary>
    public static string ResolveIncludePath(string navigationPath)
    {
        var segments = navigationPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var currentType = typeof(T);
        var resolved = new string[segments.Length];

        for (var i = 0; i < segments.Length; i++)
        {
            var property = currentType.GetProperty(segments[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                            ?? throw new QueryFlow.Abstractions.Exceptions.InvalidFieldException(navigationPath);

            resolved[i] = property.Name;
            currentType = UnwrapElementType(property.PropertyType);
        }

        return string.Join('.', resolved);
    }

    private static Type UnwrapElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType()!;
        }

        if (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
        {
            return type.GetGenericArguments().FirstOrDefault() ?? type;
        }

        return type;
    }
}
