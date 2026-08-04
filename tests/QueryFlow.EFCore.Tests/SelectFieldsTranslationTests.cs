using QueryFlow.Core;
using QueryFlow.EFCore.Tests.TestSupport;

namespace QueryFlow.EFCore.Tests;

public class SelectFieldsTranslationTests
{
    [Fact]
    public void SelectFields_pushes_projection_down_and_executes_against_sqlite()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();

        var result = db.Books.SelectFields(["Title", "Price"]).ToList();

        result.Should().HaveCount(4);
        var dict = result[0].ToFieldDictionary();
        dict.Keys.Should().BeEquivalentTo("Title", "Price");
    }

    [Fact]
    public void SelectFields_combined_with_filter_and_sort_executes_correctly()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();

        var result = db.Books
            .Filter([new QueryFlow.Abstractions.Models.FilterCondition("Genre", QueryFlow.Abstractions.Enums.FilterOperator.Equals, "Romance")])
            .Sort("Price")
            .SelectFields(["Title", "Price"])
            .ToList();

        result.Should().HaveCount(2);
        var titles = result.Select(r => (string)r.ToFieldDictionary()["Title"]!).ToList();
        titles.Should().Equal("Pride and Prejudice", "Emma");
    }
}
