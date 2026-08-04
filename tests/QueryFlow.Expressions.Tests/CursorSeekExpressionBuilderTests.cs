using QueryFlow.Abstractions.Models;
using QueryFlow.Expressions.Tests.TestSupport;

namespace QueryFlow.Expressions.Tests;

public class CursorSeekExpressionBuilderTests
{
    private static IQueryable<Widget> Data() => new List<Widget>
    {
        new() { Id = 1, Name = "A", Price = 10m },
        new() { Id = 2, Name = "B", Price = 20m },
        new() { Id = 3, Name = "C", Price = 30m },
    }.AsQueryable();

    [Fact]
    public void Forward_seek_returns_rows_strictly_after_the_given_key()
    {
        var predicate = CursorSeekExpressionBuilder.Build<Widget>([new SortField("Price")], [20m], backward: false);

        Data().Where(predicate).Select(w => w.Name).Should().Equal("C");
    }

    [Fact]
    public void Backward_seek_returns_rows_strictly_before_the_given_key()
    {
        var predicate = CursorSeekExpressionBuilder.Build<Widget>([new SortField("Price")], [20m], backward: true);

        Data().Where(predicate).Select(w => w.Name).Should().Equal("A");
    }

    [Fact]
    public void Multi_key_seek_breaks_ties_using_the_second_key()
    {
        var data = new List<Widget>
        {
            new() { Id = 1, Name = "A", Price = 10m },
            new() { Id = 2, Name = "B", Price = 10m },
            new() { Id = 3, Name = "C", Price = 20m },
        }.AsQueryable();

        var predicate = CursorSeekExpressionBuilder.Build<Widget>(
            [new SortField("Price"), new SortField("Id")],
            [10m, 1],
            backward: false);

        data.Where(predicate).Select(w => w.Name).Should().BeEquivalentTo("B", "C");
    }

    [Fact]
    public void Mismatched_lengths_throw()
    {
        var act = () => CursorSeekExpressionBuilder.Build<Widget>([new SortField("Price")], [10m, 20m], backward: false);

        act.Should().Throw<ArgumentException>();
    }
}
