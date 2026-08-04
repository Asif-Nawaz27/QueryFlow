using Microsoft.EntityFrameworkCore;
using QueryFlow.EFCore.Tests.TestSupport;
using QueryFlow.Validation;

namespace QueryFlow.EFCore.Tests;

public class IncludeTests
{
    [Fact]
    public void Include_eagerly_loads_navigation_property()
    {
        using var db = SqliteDbContextFactory.CreateSeeded();
        var config = new QueryableConfig<Author>().AllowInclude("Books");

        var authors = db.Authors.Include(["Books"], config).AsNoTracking().ToList();

        authors.Should().OnlyContain(a => a.Books.Count > 0);
    }

    [Fact]
    public void Include_rejects_navigation_not_in_allowlist()
    {
        var config = new QueryableConfig<Author>().AllowInclude("SomethingElse");
        using var db = SqliteDbContextFactory.CreateSeeded();

        var act = () => db.Authors.Include(["Books"], config).ToList();

        act.Should().Throw<QueryFlow.Abstractions.Exceptions.InvalidFieldException>();
    }
}
