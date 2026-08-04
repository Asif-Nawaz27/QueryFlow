using QueryFlow.Expressions.Caching;
using QueryFlow.Expressions.Tests.TestSupport;

namespace QueryFlow.Expressions.Tests;

public class PropertyPathCacheTests
{
    [Fact]
    public void Resolves_top_level_property_case_insensitively()
    {
        var chain = PropertyPathCache.Resolve<Widget>("name");

        chain.Should().ContainSingle(p => p.Name == "Name");
    }

    [Fact]
    public void Resolves_nested_dotted_path()
    {
        var chain = PropertyPathCache.Resolve<Widget>("Address.City");

        chain.Should().HaveCount(2);
        chain[0].Name.Should().Be("Address");
        chain[1].Name.Should().Be("City");
    }

    [Fact]
    public void TryResolve_returns_null_for_unknown_property()
    {
        PropertyPathCache.TryResolve(typeof(Widget), "NotAField").Should().BeNull();
    }

    [Fact]
    public void Resolve_throws_InvalidFieldException_for_unknown_property()
    {
        var act = () => PropertyPathCache.Resolve<Widget>("NotAField");

        act.Should().Throw<QueryFlow.Abstractions.Exceptions.InvalidFieldException>();
    }

    [Fact]
    public void Repeated_resolution_returns_consistent_results()
    {
        var first = PropertyPathCache.Resolve<Widget>("Price");
        var second = PropertyPathCache.Resolve<Widget>("Price");

        second.Should().BeEquivalentTo(first);
    }
}
