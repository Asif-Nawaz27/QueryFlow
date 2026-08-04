using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Core.Tests.TestSupport;

namespace QueryFlow.Core.Tests;

public class SelectFieldsTests
{
    [Fact]
    public void SelectFields_projects_only_requested_fields()
    {
        var result = ProductData.Sample().SelectFields(["Id", "Name"]).ToList();

        result.Should().HaveCount(5);
        var dict = result[0].ToFieldDictionary();
        dict.Keys.Should().BeEquivalentTo("Id", "Name");
    }

    [Fact]
    public void SelectFields_with_no_fields_returns_source_unchanged()
    {
        var source = ProductData.Sample();
        var result = source.SelectFields([]);

        result.Should().BeSameAs(source);
    }

    [Fact]
    public void SelectFields_rejects_unknown_field_when_allowlisted()
    {
        var config = new QueryFlow.Validation.QueryableConfig<Product>().AllowSelect(p => p.Name);

        var act = () => ProductData.Sample().SelectFields(["Price"], config).ToList();

        act.Should().Throw<InvalidFieldException>();
    }
}
