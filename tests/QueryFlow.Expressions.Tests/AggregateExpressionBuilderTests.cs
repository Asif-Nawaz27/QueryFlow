using QueryFlow.Expressions.Tests.TestSupport;

namespace QueryFlow.Expressions.Tests;

public class AggregateExpressionBuilderTests
{
    [Fact]
    public void BuildSelector_produces_a_lambda_over_the_requested_field()
    {
        var selector = AggregateExpressionBuilder.BuildSelector<Widget>("Price");

        selector.Parameters.Should().HaveCount(1);
        selector.Body.Type.Should().Be(typeof(decimal));
    }

    [Fact]
    public void BuildSelector_supports_nested_paths()
    {
        var selector = AggregateExpressionBuilder.BuildSelector<Widget>("Address.City");

        selector.Body.Type.Should().Be(typeof(string));
    }
}
