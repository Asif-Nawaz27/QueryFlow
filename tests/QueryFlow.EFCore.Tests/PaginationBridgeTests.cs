using QueryFlow.Abstractions.Models;
using QueryFlow.Core;
using QueryFlow.EFCore.Tests.TestSupport;

namespace QueryFlow.EFCore.Tests;

public class PaginationBridgeTests
{
    [Fact]
    public async Task PaginateAsync_uses_real_EF_async_execution_and_returns_correct_page()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();

        var result = await db.Books.Sort("Id").PaginateAsync(new QueryRequest { Page = 1, PageSize = 2 });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(4);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task CursorPaginateAsync_walks_all_rows_against_sqlite()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();

        var seen = new List<int>();
        string? cursor = null;

        for (var i = 0; i < 10; i++)
        {
            var page = await db.Books.CursorPaginateAsync(new QueryRequest
            {
                PageSize = 2,
                Sort = [new SortField("Id")],
                Cursor = cursor
            });

            seen.AddRange(page.Items.Select(b => b.Id));
            if (!page.HasNext)
            {
                break;
            }

            cursor = page.NextCursor;
        }

        seen.Should().HaveCount(4);
        seen.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task AggregateAsync_uses_real_EF_async_execution()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();

        var results = await db.Books.AggregateAsync(
        [
            new AggregateRequest(QueryFlow.Abstractions.Enums.AggregateFunction.Count),
            new AggregateRequest(QueryFlow.Abstractions.Enums.AggregateFunction.Sum, "Stock")
        ]);

        results[0].Value.Should().Be(4L);
        results[1].Value.Should().Be(37);
    }
}
