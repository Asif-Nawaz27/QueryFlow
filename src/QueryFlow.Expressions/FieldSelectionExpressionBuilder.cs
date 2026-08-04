using System.Linq.Expressions;
using System.Reflection;
using QueryFlow.Expressions.Caching;
using QueryFlow.Expressions.Internal;

namespace QueryFlow.Expressions;

/// <summary>
/// Projects an entity onto a subset of its top-level fields, powering the <c>?fields=</c> query
/// parameter. The projection targets a runtime-emitted type with real properties (see
/// <see cref="DynamicTypeBuilder"/>) rather than a dictionary, so providers like EF Core
/// translate it into a SQL query that only selects the requested columns instead of
/// over-fetching the whole row.
/// </summary>
public static class FieldSelectionExpressionBuilder
{
    /// <summary>
    /// Builds a projection expression from <typeparamref name="T"/> to a runtime-generated type
    /// with one property per requested field. Use <see cref="ToDictionary"/> to convert the
    /// materialized results into a uniform, JSON-friendly shape.
    /// </summary>
    public static LambdaExpression Build<T>(IReadOnlyList<string> fields)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var members = new List<(string Name, Type Type, Expression Access)>();
        foreach (var field in fields)
        {
            var propertyChain = PropertyPathCache.Resolve<T>(field);
            Expression member = parameter;
            foreach (var property in propertyChain)
            {
                member = Expression.Property(member, property);
            }

            members.Add((field, member.Type, member));
        }

        var dynamicType = DynamicTypeBuilder.GetOrCreate(members.Select(m => (m.Name, m.Type)).ToList());

        var bindings = members.Select(m =>
            Expression.Bind(dynamicType.GetProperty(m.Name)!, m.Access));

        var body = Expression.MemberInit(Expression.New(dynamicType), bindings);
        return Expression.Lambda(body, parameter);
    }

    /// <summary>
    /// Applies field selection to <paramref name="source"/>, returning a queryable of
    /// runtime-projected instances. Providers such as EF Core translate this to a SQL query that
    /// selects only the requested columns. Returns <paramref name="source"/> unchanged (via
    /// reference covariance) when <paramref name="fields"/> is empty.
    /// </summary>
    public static IQueryable<object> Apply<T>(IQueryable<T> source, IReadOnlyList<string> fields)
        where T : class
    {
        if (fields.Count == 0)
        {
            return source;
        }

        var lambda = Build<T>(fields);
        var resultType = lambda.Body.Type;
        var selectMethod = ReflectionMethodCache.Select(typeof(T), resultType);
        return (IQueryable<object>)selectMethod.Invoke(null, [source, lambda])!;
    }

    /// <summary>Converts a projected instance (produced by a lambda from <see cref="Build{T}"/>) into a plain dictionary.</summary>
    public static IReadOnlyDictionary<string, object?> ToDictionary(object projected)
    {
        var type = projected.GetType();
        var properties = PropertiesOfDynamicType.GetOrAdd(type, static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        var result = new Dictionary<string, object?>(properties.Length);
        foreach (var property in properties)
        {
            result[property.Name] = property.GetValue(projected);
        }

        return result;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, PropertyInfo[]> PropertiesOfDynamicType = new();
}
