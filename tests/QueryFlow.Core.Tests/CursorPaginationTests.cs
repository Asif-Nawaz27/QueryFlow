using QueryFlow.Abstractions.Models;
using QueryFlow.Core.Tests.TestSupport;

namespace QueryFlow.Core.Tests;

public class CursorPaginationTests
{
    [Fact]
    public void First_page_has_no_previous_cursor_and_a_next_cursor_when_more_rows_exist()
    {
        var request = new QueryRequest { PageSize = 2, Sort = [new SortField("Id")] };
        var result = ProductData.Sample().CursorPaginate(request);

        result.Items.Select(p => p.Id).Should().Equal(1, 2);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public void Walking_forward_through_all_pages_covers_every_row_exactly_once()
    {
        var seen = new List<int>();
        string? cursor = null;

        for (var i = 0; i < 10; i++)
        {
            var request = new QueryRequest { PageSize = 2, Sort = [new SortField("Id")], Cursor = cursor };
            var page = ProductData.Sample().CursorPaginate(request);
            seen.AddRange(page.Items.Select(p => p.Id));

            if (!page.HasNext)
            {
                break;
            }

            cursor = page.NextCursor;
        }

        seen.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public void Paging_backward_from_the_second_page_returns_to_the_first_page()
    {
        var firstPage = ProductData.Sample().CursorPaginate(new QueryRequest { PageSize = 2, Sort = [new SortField("Id")] });
        var secondPage = ProductData.Sample().CursorPaginate(new QueryRequest { PageSize = 2, Sort = [new SortField("Id")], Cursor = firstPage.NextCursor });

        secondPage.HasPrevious.Should().BeTrue();

        var backToFirst = ProductData.Sample().CursorPaginate(new QueryRequest { PageSize = 2, Sort = [new SortField("Id")], Cursor = secondPage.PreviousCursor });

        backToFirst.Items.Select(p => p.Id).Should().Equal(firstPage.Items.Select(p => p.Id));
    }

    [Fact]
    public void Cursor_pagination_without_sort_fields_throws()
    {
        var act = () => ProductData.Sample().CursorPaginate(new QueryRequest { PageSize = 2 });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Multi_key_sort_produces_a_stable_seek_across_ties()
    {
        var request = new QueryRequest
        {
            PageSize = 2,
            Sort = [new SortField("Category"), new SortField("Id")]
        };

        var seen = new List<(string Category, int Id)>();
        string? cursor = null;

        for (var i = 0; i < 10; i++)
        {
            var page = ProductData.Sample().CursorPaginate(request with { Cursor = cursor });
            seen.AddRange(page.Items.Select(p => (p.Category, p.Id)));

            if (!page.HasNext)
            {
                break;
            }

            cursor = page.NextCursor;
        }

        seen.Should().HaveCount(5);
        seen.Should().OnlyHaveUniqueItems();
    }
}
