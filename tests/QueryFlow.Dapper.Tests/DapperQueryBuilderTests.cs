using global::Dapper;
using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Dapper.Tests.TestSupport;
using QueryFlow.Validation;

namespace QueryFlow.Dapper.Tests;

public class DapperQueryBuilderTests
{
    [Fact]
    public void Equals_filter_produces_executable_sql()
    {
        using var connection = SqliteFixture.CreateSeeded();
        var builder = new DapperQueryBuilder<Book>();
        var request = new QueryRequest { Filters = [new FilterCondition("Genre", FilterOperator.Equals, "Romance")] };

        var query = builder.Build(request, DatabaseProvider.Sqlite);
        var sql = $"SELECT * FROM Books {query.WhereSql} {query.OrderBySql} {query.PagingSql}";
        var results = connection.Query<Book>(sql, query.Parameters).ToList();

        results.Should().HaveCount(2);
    }

    [Fact]
    public void Contains_filter_translates_to_like()
    {
        using var connection = SqliteFixture.CreateSeeded();
        var builder = new DapperQueryBuilder<Book>();
        var request = new QueryRequest { Filters = [new FilterCondition("Title", FilterOperator.Contains, "prej")] };

        var query = builder.Build(request, DatabaseProvider.Sqlite);
        var sql = $"SELECT * FROM Books {query.WhereSql}";
        var results = connection.Query<Book>(sql, query.Parameters).ToList();

        results.Should().ContainSingle(b => b.Title == "Pride and Prejudice");
    }

    [Fact]
    public void Between_and_In_filters_combine_with_and_connector()
    {
        using var connection = SqliteFixture.CreateSeeded();
        var builder = new DapperQueryBuilder<Book>();
        var request = new QueryRequest
        {
            Filters =
            [
                new FilterCondition("Price", FilterOperator.Between, Values: [5m, 10m], Connector: LogicalOperator.And),
                new FilterCondition("Genre", FilterOperator.In, Values: ["Romance", "Dystopian"])
            ]
        };

        var query = builder.Build(request, DatabaseProvider.Sqlite);
        var sql = $"SELECT * FROM Books {query.WhereSql}";
        var results = connection.Query<Book>(sql, query.Parameters).ToList();

        results.Select(b => b.Title).Should().BeEquivalentTo("Pride and Prejudice", "1984", "Animal Farm");
    }

    [Fact]
    public void Search_matches_across_configured_columns()
    {
        using var connection = SqliteFixture.CreateSeeded();
        var config = new QueryableConfig<Book>().Searchable(b => b.Title);
        var builder = new DapperQueryBuilder<Book>(config);
        var request = new QueryRequest { Search = "emma" };

        var query = builder.Build(request, DatabaseProvider.Sqlite);
        var sql = $"SELECT * FROM Books {query.WhereSql}";
        var results = connection.Query<Book>(sql, query.Parameters).ToList();

        results.Should().ContainSingle(b => b.Title == "Emma");
    }

    [Fact]
    public void Sort_and_offset_paging_produce_correct_page()
    {
        using var connection = SqliteFixture.CreateSeeded();
        var builder = new DapperQueryBuilder<Book>();
        var request = new QueryRequest { Sort = [new SortField("Id")], Page = 2, PageSize = 2 };

        var query = builder.Build(request, DatabaseProvider.Sqlite);
        var sql = $"SELECT * FROM Books {query.WhereSql} {query.OrderBySql} {query.PagingSql}";
        var results = connection.Query<Book>(sql, query.Parameters).ToList();

        results.Select(b => b.Id).Should().Equal(3, 4);
    }

    [Fact]
    public void SqlServer_paging_uses_offset_fetch_syntax()
    {
        var builder = new DapperQueryBuilder<Book>();
        var request = new QueryRequest { Sort = [new SortField("Id")], Page = 1, PageSize = 10 };

        var query = builder.Build(request, DatabaseProvider.SqlServer);

        query.PagingSql.Should().Contain("OFFSET").And.Contain("FETCH NEXT");
    }

    [Fact]
    public void FilterGroup_nested_and_or_produces_correct_sql_and_results()
    {
        using var connection = SqliteFixture.CreateSeeded();
        var builder = new DapperQueryBuilder<Book>();
        var request = new QueryRequest
        {
            FilterGroup = new FilterGroup(
                LogicalOperator.Or,
                Conditions: [new FilterCondition("Stock", FilterOperator.Equals, 0)],
                Groups:
                [
                    new FilterGroup(LogicalOperator.And, Conditions:
                    [
                        new FilterCondition("Genre", FilterOperator.Equals, "Romance"),
                        new FilterCondition("Price", FilterOperator.LessThan, 10m)
                    ])
                ])
        };

        var query = builder.Build(request, DatabaseProvider.Sqlite);
        var sql = $"SELECT * FROM Books {query.WhereSql}";
        var results = connection.Query<Book>(sql, query.Parameters).ToList();

        results.Select(b => b.Title).Should().BeEquivalentTo("Pride and Prejudice", "1984");
    }

    [Fact]
    public void Unknown_field_throws_InvalidFieldException()
    {
        var builder = new DapperQueryBuilder<Book>();
        var request = new QueryRequest { Filters = [new FilterCondition("NotAColumn", FilterOperator.Equals, "x")] };

        var act = () => builder.Build(request, DatabaseProvider.Sqlite);

        act.Should().Throw<InvalidFieldException>();
    }

    [Fact]
    public void Column_map_translates_the_field_name_into_the_physical_column_in_the_emitted_sql()
    {
        // Book.Genre (the allowlisted, reflectable field) is stored as "genre_category" in the database.
        var config = new QueryableConfig<Book>().AllowFilter(b => b.Genre, FilterOperator.Equals);
        var builder = new DapperQueryBuilder<Book>(config, new Dictionary<string, string> { ["Genre"] = "genre_category" });
        var request = new QueryRequest { Filters = [new FilterCondition("Genre", FilterOperator.Equals, "Dystopian")] };

        var query = builder.Build(request, DatabaseProvider.Sqlite);

        query.WhereSql.Should().Contain("genre_category").And.NotContain("Genre =");
    }
}
