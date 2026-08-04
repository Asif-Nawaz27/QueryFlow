using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Core.Tests.TestSupport;

namespace QueryFlow.Core.Tests;

public class FilterTests
{
    [Fact]
    public void Equals_filters_matching_rows()
    {
        var result = ProductData.Sample()
            .Filter([new FilterCondition("Category", FilterOperator.Equals, "Electronics")])
            .ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Category == "Electronics");
    }

    [Fact]
    public void GreaterThan_and_LessThan_combine_with_and_connector()
    {
        var result = ProductData.Sample()
            .Filter([
                new FilterCondition("Price", FilterOperator.GreaterThan, 10m, Connector: LogicalOperator.And),
                new FilterCondition("Price", FilterOperator.LessThan, 40m)
            ])
            .ToList();

        result.Select(p => p.Name).Should().BeEquivalentTo("Gadget", "Doohickey");
    }

    [Fact]
    public void Between_is_inclusive()
    {
        var result = ProductData.Sample()
            .Filter([new FilterCondition("Price", FilterOperator.Between, Values: [10m, 30m])])
            .ToList();

        result.Select(p => p.Name).Should().BeEquivalentTo("Gadget", "Doohickey");
    }

    [Fact]
    public void In_matches_any_of_the_supplied_values()
    {
        var result = ProductData.Sample()
            .Filter([new FilterCondition("Category", FilterOperator.In, Values: ["Hardware", "Misc"])])
            .ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public void NotIn_excludes_the_supplied_values()
    {
        var result = ProductData.Sample()
            .Filter([new FilterCondition("Category", FilterOperator.NotIn, Values: ["Hardware", "Misc"])])
            .ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Category == "Electronics");
    }

    [Fact]
    public void Contains_is_case_insensitive()
    {
        var result = ProductData.Sample()
            .Filter([new FilterCondition("Name", FilterOperator.Contains, "GIZ")])
            .ToList();

        result.Should().ContainSingle(p => p.Name == "Gizmo");
    }

    [Fact]
    public void StartsWith_and_EndsWith_work()
    {
        var starts = ProductData.Sample().Filter([new FilterCondition("Name", FilterOperator.StartsWith, "Wid")]).ToList();
        var ends = ProductData.Sample().Filter([new FilterCondition("Name", FilterOperator.EndsWith, "get")]).ToList();

        starts.Should().ContainSingle(p => p.Name == "Widget");
        ends.Select(p => p.Name).Should().BeEquivalentTo("Widget", "Gadget");
    }

    [Fact]
    public void IsNull_and_IsNotNull_work_on_nullable_reference_property()
    {
        var nulls = ProductData.Sample().Filter([new FilterCondition("Description", FilterOperator.IsNull)]).ToList();
        var notNulls = ProductData.Sample().Filter([new FilterCondition("Description", FilterOperator.IsNotNull)]).ToList();

        nulls.Should().ContainSingle(p => p.Name == "Gizmo");
        notNulls.Should().HaveCount(4);
    }

    [Fact]
    public void Or_connector_combines_conditions_with_logical_or()
    {
        var result = ProductData.Sample()
            .Filter([
                new FilterCondition("Category", FilterOperator.Equals, "Misc", Connector: LogicalOperator.Or),
                new FilterCondition("Category", FilterOperator.Equals, "Hardware")
            ])
            .ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public void FilterGroup_supports_nested_and_or_logic()
    {
        // (Category == Electronics AND Price > 40) OR IsActive == false
        var group = new FilterGroup(
            LogicalOperator.Or,
            Conditions: [new FilterCondition("IsActive", FilterOperator.Equals, false)],
            Groups:
            [
                new FilterGroup(LogicalOperator.And, Conditions:
                [
                    new FilterCondition("Category", FilterOperator.Equals, "Electronics"),
                    new FilterCondition("Price", FilterOperator.GreaterThan, 40m)
                ])
            ]);

        var result = ProductData.Sample().Filter(group).ToList();

        result.Select(p => p.Name).Should().BeEquivalentTo("Gizmo");
    }

    [Fact]
    public void Unknown_field_throws_InvalidFieldException()
    {
        var act = () => ProductData.Sample()
            .Filter([new FilterCondition("NotAField", FilterOperator.Equals, "x")])
            .ToList();

        act.Should().Throw<InvalidFieldException>();
    }

    [Fact]
    public void Disallowed_operator_throws_InvalidOperatorException_when_config_restricts_it()
    {
        var config = new QueryFlow.Validation.QueryableConfig<Product>()
            .AllowFilter(p => p.Name, FilterOperator.Equals);

        var act = () => ProductData.Sample()
            .Filter([new FilterCondition("Name", FilterOperator.Contains, "a")], config)
            .ToList();

        act.Should().Throw<InvalidOperatorException>();
    }

    [Fact]
    public void Contains_on_non_string_property_throws_InvalidOperatorException()
    {
        var act = () => ProductData.Sample()
            .Filter([new FilterCondition("Price", FilterOperator.Contains, "9")])
            .ToList();

        act.Should().Throw<InvalidOperatorException>();
    }
}
