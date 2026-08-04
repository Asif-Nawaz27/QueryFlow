using System.Text.Json;
using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Core.Cursor;
using QueryFlow.Expressions.Caching;

namespace QueryFlow.Core.Internal;

internal static class CursorKeyExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<CursorKeyValue> ExtractKeys<T>(T item, IReadOnlyList<SortField> sortFields)
    {
        var result = new List<CursorKeyValue>(sortFields.Count);
        foreach (var sortField in sortFields)
        {
            var chain = PropertyPathCache.Resolve<T>(sortField.Field);
            object? current = item;
            var propertyType = typeof(T);
            foreach (var property in chain)
            {
                current = current is null ? null : property.GetValue(current);
                propertyType = property.PropertyType;
            }

            var json = current is null ? null : JsonSerializer.Serialize(current, propertyType, JsonOptions);
            result.Add(new CursorKeyValue(sortField.Field, json, propertyType.AssemblyQualifiedName));
        }

        return result;
    }

    public static IReadOnlyList<object?> ToValues(IReadOnlyList<CursorKeyValue> keys)
    {
        var result = new List<object?>(keys.Count);
        foreach (var key in keys)
        {
            if (key.Json is null || key.TypeName is null)
            {
                result.Add(null);
                continue;
            }

            var type = Type.GetType(key.TypeName)
                       ?? throw new InvalidCursorException($"Unable to resolve cursor key type '{key.TypeName}'.");
            result.Add(JsonSerializer.Deserialize(key.Json, type, JsonOptions));
        }

        return result;
    }
}
