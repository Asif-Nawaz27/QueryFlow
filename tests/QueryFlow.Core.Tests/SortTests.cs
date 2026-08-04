using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Core.Tests.TestSupport;

namespace QueryFlow.Core.Tests;

public class SortTests
{
    [Fact]
    public void Single_column_ascending_sort()
    {
        var result = ProductData.Sample().Sort([new SortField("Price")]).ToList();

        result.Select(p => p.Price).Should().BeInAscendingOrder();
    }

    [Fact]
    public void Single_column_descending_sort()
    {
        var result = ProductData.Sample().Sort([new SortField("Price", SortDirection.Descending)]).ToList();

        result.Select(p => p.Price).Should().BeInDescendingOrder();
    }

    [Fact]
    public void Multi_column_sort_via_string_tokens()
    {
        var result = ProductData.Sample().Sort("Category,-Price").ToList();

        result.Should().BeInAscendingOrder(p => p.Category);
        var grouped = result.GroupBy(p => p.Category);
        foreach (var group in grouped)
        {
            group.Select(p => p.Price).Should().BeInDescendingOrder();
        }
    }

    [Fact]
    public void SortField_Parse_handles_prefix_tokens()
    {
        SortField.Parse("-CreatedDate").Should().Be(new SortField("CreatedDate", SortDirection.Descending));
        SortField.Parse("LastName").Should().Be(new SortField("LastName", SortDirection.Ascending));
        SortField.Parse("+LastName").Should().Be(new SortField("LastName", SortDirection.Ascending));
    }

    [Fact]
    public void Unknown_sort_field_throws_when_allowlist_restricts()
    {
        var config = new QueryFlow.Validation.QueryableConfig<Product>().AllowSort(p => p.Name);

        var act = () => ProductData.Sample().Sort([new SortField("Price")], config).ToList();

        act.Should().Throw<InvalidFieldException>();
    }
}
