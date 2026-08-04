using QueryFlow.Expressions.Tests.TestSupport;

namespace QueryFlow.Expressions.Tests;

public class FieldSelectionExpressionBuilderTests
{
    private static IQueryable<Widget> Data() => new List<Widget>
    {
        new() { Id = 1, Name = "Alpha", Price = 10m },
        new() { Id = 2, Name = "Beta", Price = 20m },
    }.AsQueryable();

    [Fact]
    public void Apply_projects_only_requested_fields()
    {
        var result = FieldSelectionExpressionBuilder.Apply(Data(), ["Id", "Name"]).ToList();

        result.Should().HaveCount(2);
        var dict = FieldSelectionExpressionBuilder.ToDictionary(result[0]);
        dict.Keys.Should().BeEquivalentTo("Id", "Name");
        dict["Name"].Should().Be("Alpha");
    }

    [Fact]
    public void Apply_with_no_fields_returns_source_unchanged()
    {
        var source = Data();
        var result = FieldSelectionExpressionBuilder.Apply(source, []);

        result.Should().BeSameAs(source);
    }

    [Fact]
    public void Repeated_calls_with_the_same_field_set_reuse_the_cached_dynamic_type()
    {
        var first = FieldSelectionExpressionBuilder.Apply(Data(), ["Id", "Name"]).ToList();
        var second = FieldSelectionExpressionBuilder.Apply(Data(), ["Id", "Name"]).ToList();

        first[0].GetType().Should().Be(second[0].GetType());
    }

    [Fact]
    public void Different_field_sets_produce_different_dynamic_types()
    {
        var a = FieldSelectionExpressionBuilder.Apply(Data(), ["Id"]).ToList();
        var b = FieldSelectionExpressionBuilder.Apply(Data(), ["Id", "Name"]).ToList();

        a[0].GetType().Should().NotBe(b[0].GetType());
    }
}
