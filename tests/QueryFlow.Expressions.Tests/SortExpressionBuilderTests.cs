using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Models;
using QueryFlow.Expressions.Tests.TestSupport;

namespace QueryFlow.Expressions.Tests;

public class SortExpressionBuilderTests
{
    private static IQueryable<Widget> Data() => new List<Widget>
    {
        new() { Id = 1, Name = "Charlie", Price = 30m },
        new() { Id = 2, Name = "Alpha", Price = 10m },
        new() { Id = 3, Name = "Bravo", Price = 20m },
    }.AsQueryable();

    [Fact]
    public void No_sort_fields_returns_source_unchanged()
    {
        var source = Data();
        var result = SortExpressionBuilder.Apply(source, []);

        result.Should().BeSameAs(source);
    }

    [Fact]
    public void Single_ascending_sort()
    {
        var result = SortExpressionBuilder.Apply(Data(), [new SortField("Price")]).ToList();

        result.Select(w => w.Name).Should().Equal("Alpha", "Bravo", "Charlie");
    }

    [Fact]
    public void Single_descending_sort()
    {
        var result = SortExpressionBuilder.Apply(Data(), [new SortField("Name", SortDirection.Descending)]).ToList();

        result.Select(w => w.Name).Should().Equal("Charlie", "Bravo", "Alpha");
    }

    [Fact]
    public void Multi_key_sort_applies_ThenBy_for_subsequent_keys()
    {
        var data = new List<Widget>
        {
            new() { Id = 1, Name = "A", Price = 10m, Status = WidgetStatus.Active },
            new() { Id = 2, Name = "B", Price = 5m, Status = WidgetStatus.Active },
            new() { Id = 3, Name = "C", Price = 1m, Status = WidgetStatus.Draft },
        }.AsQueryable();

        var result = SortExpressionBuilder.Apply(data, [new SortField("Status"), new SortField("Price")]).ToList();

        result.Select(w => w.Name).Should().Equal("C", "B", "A");
    }
}
