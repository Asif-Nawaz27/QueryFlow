using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Models;
using QueryFlow.Expressions.Tests.TestSupport;

namespace QueryFlow.Expressions.Tests;

public class FilterExpressionBuilderTests
{
    private static readonly Guid KnownGuid = Guid.NewGuid();

    private static IQueryable<Widget> Data() => new List<Widget>
    {
        new() { Id = 1, Name = "Alpha", Price = 10m, Quantity = 5, ExternalId = KnownGuid, Status = WidgetStatus.Active, Address = new Address { City = "Denver" } },
        new() { Id = 2, Name = "Beta", Price = 20m, Quantity = null, ExternalId = Guid.NewGuid(), Status = WidgetStatus.Draft, Address = null },
        new() { Id = 3, Name = "Gamma", Price = 30m, Quantity = 0, ExternalId = Guid.NewGuid(), Status = WidgetStatus.Retired, Address = new Address { City = "Austin" } },
    }.AsQueryable();

    [Fact]
    public void Filters_on_enum_field_via_string_value()
    {
        var predicate = FilterExpressionBuilder.Build<Widget>([new FilterCondition("Status", FilterOperator.Equals, "Active")]);

        Data().Where(predicate).Should().ContainSingle(w => w.Name == "Alpha");
    }

    [Fact]
    public void Filters_on_guid_field_via_string_value()
    {
        var predicate = FilterExpressionBuilder.Build<Widget>([new FilterCondition("ExternalId", FilterOperator.Equals, KnownGuid.ToString())]);

        Data().Where(predicate).Should().ContainSingle(w => w.Name == "Alpha");
    }

    [Fact]
    public void Filters_on_nullable_int_IsNull()
    {
        var predicate = FilterExpressionBuilder.Build<Widget>([new FilterCondition("Quantity", FilterOperator.IsNull)]);

        Data().Where(predicate).Should().ContainSingle(w => w.Name == "Beta");
    }

    [Fact]
    public void Filters_on_nested_property_path()
    {
        var predicate = FilterExpressionBuilder.Build<Widget>([new FilterCondition("Address.City", FilterOperator.Equals, "Austin")]);

        Data().Where(predicate).Should().ContainSingle(w => w.Name == "Gamma");
    }

    [Fact]
    public void Between_is_inclusive_on_both_bounds()
    {
        var predicate = FilterExpressionBuilder.Build<Widget>([new FilterCondition("Price", FilterOperator.Between, Values: [10m, 20m])]);

        Data().Where(predicate).Should().HaveCount(2);
    }

    [Fact]
    public void FilterGroup_nested_or_and_produces_expected_matches()
    {
        var group = new FilterGroup(
            LogicalOperator.Or,
            Conditions: [new FilterCondition("Status", FilterOperator.Equals, "Draft")],
            Groups: [new FilterGroup(LogicalOperator.And, Conditions: [new FilterCondition("Price", FilterOperator.GreaterThan, 25m)])]);

        var predicate = FilterExpressionBuilder.Build<Widget>(group);

        Data().Where(predicate).Select(w => w.Name).Should().BeEquivalentTo("Beta", "Gamma");
    }

    [Fact]
    public void IsNull_on_non_nullable_value_type_throws()
    {
        var act = () => FilterExpressionBuilder.Build<Widget>([new FilterCondition("Price", FilterOperator.IsNull)]);

        act.Should().Throw<QueryFlow.Abstractions.Exceptions.InvalidOperatorException>();
    }
}
