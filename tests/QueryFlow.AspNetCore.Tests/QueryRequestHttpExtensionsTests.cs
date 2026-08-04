using Microsoft.AspNetCore.Http;

namespace QueryFlow.AspNetCore.Tests;

public class QueryRequestHttpExtensionsTests
{
    private static HttpRequest BuildRequest(string queryString)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(queryString);
        return context.Request;
    }

    [Fact]
    public void Parses_all_standard_query_parameters()
    {
        var request = BuildRequest("?filter=age gt 18&sort=LastName,-CreatedDate&search=john&fields=id,name&include=orders&page=2&pageSize=10");

        var result = request.ToQueryRequest();

        result.Filters.Should().ContainSingle();
        result.Sort.Should().HaveCount(2);
        result.Search.Should().Be("john");
        result.Fields.Should().Equal("id", "name");
        result.Includes.Should().Equal("orders");
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void Missing_parameters_produce_empty_defaults()
    {
        var request = BuildRequest("");

        var result = request.ToQueryRequest();

        result.Filters.Should().BeEmpty();
        result.Sort.Should().BeEmpty();
        result.Fields.Should().BeEmpty();
        result.Includes.Should().BeEmpty();
        result.Search.Should().BeNull();
        result.Page.Should().BeNull();
        result.PageSize.Should().BeNull();
        result.Cursor.Should().BeNull();
    }

    [Fact]
    public void Reads_cursor_parameter()
    {
        var request = BuildRequest("?cursor=abc123.def456");

        var result = request.ToQueryRequest();

        result.Cursor.Should().Be("abc123.def456");
        result.UseCursorPagination.Should().BeTrue();
    }
}
