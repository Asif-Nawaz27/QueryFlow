using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Abstractions.Options;
using QueryFlow.Core.Tests.TestSupport;

namespace QueryFlow.Core.Tests;

public class PaginationTests
{
    [Fact]
    public void Paginate_returns_correct_page_and_metadata()
    {
        var result = ProductData.Sample().Sort([new SortField("Id")]).Paginate(new QueryRequest { Page = 2, PageSize = 2 });

        result.Items.Select(p => p.Id).Should().Equal(3, 4);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Paginate_first_page_has_no_previous()
    {
        var result = ProductData.Sample().Sort([new SortField("Id")]).Paginate(new QueryRequest { Page = 1, PageSize = 2 });

        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task PaginateAsync_matches_sync_result()
    {
        var request = new QueryRequest { Page = 1, PageSize = 3 };
        var sync = ProductData.Sample().Sort([new SortField("Id")]).Paginate(request);
        var async = await ProductData.Sample().Sort([new SortField("Id")]).PaginateAsync(request);

        async.Items.Select(p => p.Id).Should().Equal(sync.Items.Select(p => p.Id));
        async.TotalCount.Should().Be(sync.TotalCount);
    }

    [Fact]
    public void PageSize_exceeding_max_throws_via_validator()
    {
        var options = new QueryFlowOptions { MaxPageSize = 10 };
        var act = () => QueryFlow.Validation.QueryRequestValidator.Validate(
            new QueryRequest { PageSize = 500 },
            QueryFlow.Validation.QueryableConfig<Product>.Default,
            options);

        act.Should().Throw<PageSizeExceededException>();
    }

    [Fact]
    public void Paginate_clamps_page_size_to_configured_max()
    {
        var options = new QueryFlowOptions { MaxPageSize = 2 };
        var result = ProductData.Sample().Sort([new SortField("Id")]).Paginate(new QueryRequest { Page = 1, PageSize = 500 }, options);

        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }
}
