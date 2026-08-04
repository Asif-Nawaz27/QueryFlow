using System.Text.Json;
using QueryFlow.Expressions.Internal;

namespace QueryFlow.Expressions.Tests.Internal;

public class ValueConverterTests
{
    [Fact]
    public void Converts_null_to_null()
    {
        ValueConverter.Convert(null, typeof(int?)).Should().BeNull();
    }

    [Fact]
    public void Passes_through_already_matching_type()
    {
        ValueConverter.Convert(42, typeof(int)).Should().Be(42);
    }

    [Fact]
    public void Converts_string_to_enum_case_insensitively()
    {
        ValueConverter.Convert("active", typeof(TestSupport.WidgetStatus)).Should().Be(TestSupport.WidgetStatus.Active);
    }

    [Fact]
    public void Converts_string_to_guid()
    {
        var guid = Guid.NewGuid();
        ValueConverter.Convert(guid.ToString(), typeof(Guid)).Should().Be(guid);
    }

    [Fact]
    public void Converts_string_to_datetime()
    {
        var result = ValueConverter.Convert("2024-01-15T00:00:00", typeof(DateTime));
        result.Should().Be(new DateTime(2024, 1, 15));
    }

    [Fact]
    public void Converts_numeric_string_to_decimal()
    {
        ValueConverter.Convert("19.99", typeof(decimal)).Should().Be(19.99m);
    }

    [Fact]
    public void Unwraps_JsonElement_string()
    {
        var element = JsonDocument.Parse("\"hello\"").RootElement;
        ValueConverter.Convert(element, typeof(string)).Should().Be("hello");
    }

    [Fact]
    public void Unwraps_JsonElement_number_to_int()
    {
        var element = JsonDocument.Parse("42").RootElement;
        ValueConverter.Convert(element, typeof(int)).Should().Be(42);
    }

    [Fact]
    public void Unwraps_JsonElement_bool()
    {
        var element = JsonDocument.Parse("true").RootElement;
        ValueConverter.Convert(element, typeof(bool)).Should().Be(true);
    }

    [Fact]
    public void Unwraps_JsonElement_null_to_null()
    {
        var element = JsonDocument.Parse("null").RootElement;
        ValueConverter.Convert(element, typeof(string)).Should().BeNull();
    }

    [Fact]
    public void Converts_to_nullable_underlying_type()
    {
        ValueConverter.Convert("5", typeof(int?)).Should().Be(5);
    }
}
