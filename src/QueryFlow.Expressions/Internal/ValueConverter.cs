using System.Globalization;
using System.Text.Json;

namespace QueryFlow.Expressions.Internal;

/// <summary>
/// Converts loosely-typed filter values (strings from a query string, boxed primitives or
/// <see cref="JsonElement"/> from a JSON body) into the CLR type of the target property.
/// </summary>
internal static class ValueConverter
{
    public static object? Convert(object? value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            value = UnwrapJsonElement(element, underlyingType);
            if (value is null)
            {
                return null;
            }
        }

        if (underlyingType.IsInstanceOfType(value))
        {
            return value;
        }

        if (underlyingType.IsEnum)
        {
            return value is string enumString
                ? Enum.Parse(underlyingType, enumString, ignoreCase: true)
                : Enum.ToObject(underlyingType, value);
        }

        if (underlyingType == typeof(Guid))
        {
            return Guid.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)!);
        }

        if (underlyingType == typeof(DateTime))
        {
            return value is DateTime dt
                ? dt
                : DateTime.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (underlyingType == typeof(DateTimeOffset))
        {
            return value is DateTimeOffset dto
                ? dto
                : DateTimeOffset.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (underlyingType == typeof(TimeSpan))
        {
            return TimeSpan.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
        }

        if (underlyingType == typeof(bool) && value is string boolString)
        {
            return bool.Parse(boolString);
        }

        return System.Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
    }

    private static object? UnwrapJsonElement(JsonElement element, Type underlyingType)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (underlyingType == typeof(string) || underlyingType.IsEnum || underlyingType == typeof(Guid) ||
            underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset) || underlyingType == typeof(TimeSpan))
        {
            return element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
        }

        if (underlyingType == typeof(bool))
        {
            return element.GetBoolean();
        }

        if (underlyingType == typeof(int)) return element.GetInt32();
        if (underlyingType == typeof(long)) return element.GetInt64();
        if (underlyingType == typeof(short)) return element.GetInt16();
        if (underlyingType == typeof(byte)) return element.GetByte();
        if (underlyingType == typeof(double)) return element.GetDouble();
        if (underlyingType == typeof(float)) return element.GetSingle();
        if (underlyingType == typeof(decimal)) return element.GetDecimal();

        return element.ToString();
    }
}
