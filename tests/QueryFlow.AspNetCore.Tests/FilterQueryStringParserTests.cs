using QueryFlow.Abstractions.Enums;

namespace QueryFlow.AspNetCore.Tests;

public class FilterQueryStringParserTests
{
    [Fact]
    public void Parses_a_single_condition()
    {
        var result = FilterQueryStringParser.Parse("age gt 18");

        result.Should().ContainSingle();
        result[0].Field.Should().Be("age");
        result[0].Operator.Should().Be(FilterOperator.GreaterThan);
        result[0].Value.Should().Be(18L);
    }

    [Fact]
    public void Parses_quoted_string_values()
    {
        var result = FilterQueryStringParser.Parse("""city eq "New York" """);

        result[0].Value.Should().Be("New York");
    }

    [Fact]
    public void Parses_multiple_conditions_joined_by_and()
    {
        var result = FilterQueryStringParser.Parse("age gt 18 and isActive eq true");

        result.Should().HaveCount(2);
        result[0].Connector.Should().Be(LogicalOperator.And);
        result[0].Field.Should().Be("age");
        result[1].Field.Should().Be("isActive");
        result[1].Value.Should().Be(true);
    }

    [Fact]
    public void Parses_or_connector()
    {
        var result = FilterQueryStringParser.Parse("category eq Hardware or category eq Misc");

        result[0].Connector.Should().Be(LogicalOperator.Or);
    }

    [Fact]
    public void Parses_between_with_comma_separated_values()
    {
        var result = FilterQueryStringParser.Parse("price between 10,50");

        result[0].Operator.Should().Be(FilterOperator.Between);
        result[0].Values.Should().Equal(10L, 50L);
    }

    [Fact]
    public void Parses_in_with_comma_separated_values()
    {
        var result = FilterQueryStringParser.Parse("category in Hardware,Misc");

        result[0].Operator.Should().Be(FilterOperator.In);
        result[0].Values.Should().Equal("Hardware", "Misc");
    }

    [Fact]
    public void Parses_isnull_without_a_value()
    {
        var result = FilterQueryStringParser.Parse("description isnull");

        result[0].Operator.Should().Be(FilterOperator.IsNull);
        result[0].Value.Should().BeNull();
    }

    [Fact]
    public void Empty_or_blank_expression_returns_no_conditions()
    {
        FilterQueryStringParser.Parse(null).Should().BeEmpty();
        FilterQueryStringParser.Parse("  ").Should().BeEmpty();
    }

    [Fact]
    public void Unknown_operator_throws_FormatException()
    {
        var act = () => FilterQueryStringParser.Parse("age bogus 18");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Incomplete_expression_throws_FormatException()
    {
        var act = () => FilterQueryStringParser.Parse("age gt");

        act.Should().Throw<FormatException>();
    }
}
