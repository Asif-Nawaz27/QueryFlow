using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Models;
using QueryFlow.Core;
using QueryFlow.EFCore.Tests.TestSupport;
using QueryFlow.Validation;

namespace QueryFlow.EFCore.Tests;

public class FilterSortSearchTranslationTests
{
    [Fact]
    public void Equals_filter_translates_and_executes_against_sqlite()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();

        var result = db.Books.Filter([new FilterCondition("Genre", FilterOperator.Equals, "Dystopian")]).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Contains_filter_translates_to_sql_like()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();

        var result = db.Books.Filter([new FilterCondition("Title", FilterOperator.Contains, "prej")]).ToList();

        result.Should().ContainSingle(b => b.Title == "Pride and Prejudice");
    }

    [Fact]
    public void GreaterThan_on_string_field_translates_via_string_Compare()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();
        var config = new QueryableConfig<Book>().AllowFilter(b => b.Title, FilterOperator.GreaterThan);

        var result = db.Books.Filter([new FilterCondition("Title", FilterOperator.GreaterThan, "M")], config).ToList();

        result.Select(b => b.Title).Should().BeEquivalentTo("Pride and Prejudice");
    }

    [Fact]
    public void Between_filter_translates_and_executes()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();

        var result = db.Books.Filter([new FilterCondition("Price", FilterOperator.Between, Values: [7m, 10m])]).ToList();

        result.Select(b => b.Title).Should().BeEquivalentTo("Pride and Prejudice", "1984");
    }

    [Fact]
    public void In_filter_translates_and_executes()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();

        var result = db.Books.Filter([new FilterCondition("Genre", FilterOperator.In, Values: ["Romance"])]).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Sort_by_multiple_columns_translates_and_executes()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();

        var result = db.Books.Sort("Genre,-Price").ToList();

        result.Should().BeInAscendingOrder(b => b.Genre);
    }

    [Fact]
    public void Search_across_configured_fields_translates_and_executes()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();
        var config = new QueryableConfig<Book>().Searchable(b => b.Title);

        var result = db.Books.Search("emma", config).ToList();

        result.Should().ContainSingle(b => b.Title == "Emma");
    }
}
