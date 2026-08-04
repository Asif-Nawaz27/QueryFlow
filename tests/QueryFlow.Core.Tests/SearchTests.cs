using QueryFlow.Core.Tests.TestSupport;
using QueryFlow.Validation;

namespace QueryFlow.Core.Tests;

public class SearchTests
{
    private static readonly QueryableConfig<Product> Config = new QueryableConfig<Product>()
        .Searchable(p => p.Name)
        .Searchable(p => p.Description);

    [Fact]
    public void Search_matches_across_configured_fields_case_insensitively()
    {
        var result = ProductData.Sample().Search("gadget", Config).ToList();

        result.Should().ContainSingle(p => p.Name == "Gadget");
    }

    [Fact]
    public void Search_matches_description_field_too()
    {
        var result = ProductData.Sample().Search("fancy", Config).ToList();

        result.Should().ContainSingle(p => p.Name == "Gadget");
    }

    [Fact]
    public void Blank_search_term_is_a_no_op()
    {
        var result = ProductData.Sample().Search("  ", Config).ToList();

        result.Should().HaveCount(5);
    }

    [Fact]
    public void No_searchable_fields_configured_is_a_no_op()
    {
        var result = ProductData.Sample().Search("gadget", QueryableConfig<Product>.Default).ToList();

        result.Should().HaveCount(5);
    }
}
