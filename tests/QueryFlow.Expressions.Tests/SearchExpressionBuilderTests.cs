using QueryFlow.Expressions.Tests.TestSupport;

namespace QueryFlow.Expressions.Tests;

public class SearchExpressionBuilderTests
{
    private static IQueryable<Widget> Data() => new List<Widget>
    {
        new() { Id = 1, Name = "Widget Alpha" },
        new() { Id = 2, Name = "Gadget Beta" },
    }.AsQueryable();

    [Fact]
    public void Returns_null_predicate_for_blank_term()
    {
        SearchExpressionBuilder.Build<Widget>("  ", ["Name"]).Should().BeNull();
    }

    [Fact]
    public void Returns_null_predicate_when_no_searchable_fields()
    {
        SearchExpressionBuilder.Build<Widget>("widget", []).Should().BeNull();
    }

    [Fact]
    public void Matches_case_insensitively()
    {
        var predicate = SearchExpressionBuilder.Build<Widget>("WIDGET", ["Name"])!;

        Data().Where(predicate).Should().ContainSingle(w => w.Name == "Widget Alpha");
    }

    [Fact]
    public void Skips_non_string_fields_silently()
    {
        var predicate = SearchExpressionBuilder.Build<Widget>("10", ["Price", "Name"]);

        predicate.Should().NotBeNull();
        Data().Where(predicate!).Should().BeEmpty();
    }
}
