using QueryFlow.Abstractions.Enums;
using QueryFlow.Validation.Tests.TestSupport;

namespace QueryFlow.Validation.Tests;

public class QueryableConfigTests
{
    [Fact]
    public void Default_config_allows_any_real_property_with_type_appropriate_operators()
    {
        var config = QueryableConfig<Customer>.Default;

        config.IsFilterAllowed("Name", FilterOperator.Contains).Should().BeTrue();
        config.IsFilterAllowed("Balance", FilterOperator.GreaterThan).Should().BeTrue();
        config.IsFilterAllowed("Balance", FilterOperator.Contains).Should().BeFalse();
        config.IsFilterAllowed("NotAField", FilterOperator.Equals).Should().BeFalse();
    }

    [Fact]
    public void Default_config_allows_sort_and_select_on_any_real_property()
    {
        var config = QueryableConfig<Customer>.Default;

        config.IsSortAllowed("Balance").Should().BeTrue();
        config.IsSortAllowed("NotAField").Should().BeFalse();
        config.IsSelectAllowed("Email").Should().BeTrue();
    }

    [Fact]
    public void Calling_AllowFilter_switches_filtering_into_strict_allowlist_mode()
    {
        var config = new QueryableConfig<Customer>().AllowFilter(c => c.Name);

        config.IsFilterAllowed("Name", FilterOperator.Contains).Should().BeTrue();
        config.IsFilterAllowed("Balance", FilterOperator.GreaterThan).Should().BeFalse("Balance was never explicitly allowlisted once allowlist mode is active");
    }

    [Fact]
    public void AllowFilter_with_explicit_operators_restricts_to_only_those_operators()
    {
        var config = new QueryableConfig<Customer>().AllowFilter(c => c.Name, FilterOperator.Equals);

        config.IsFilterAllowed("Name", FilterOperator.Equals).Should().BeTrue();
        config.IsFilterAllowed("Name", FilterOperator.Contains).Should().BeFalse();
    }

    [Fact]
    public void Searchable_only_accepts_string_properties()
    {
        var config = new QueryableConfig<Customer>();

        var act = () => config.Searchable("Balance");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Sortable_field_not_in_allowlist_is_rejected_once_sort_allowlist_is_active()
    {
        var config = new QueryableConfig<Customer>().AllowSort(c => c.Name);

        config.IsSortAllowed("Name").Should().BeTrue();
        config.IsSortAllowed("Balance").Should().BeFalse();
    }

    [Fact]
    public void AllowInclude_restricts_navigation_properties()
    {
        var config = new QueryableConfig<Customer>().AllowInclude("Orders");

        config.IsIncludeAllowed("Orders").Should().BeTrue();
        config.IsIncludeAllowed("Payments").Should().BeFalse();
    }

    [Fact]
    public void IsKnownField_reflects_real_properties_regardless_of_allowlist_state()
    {
        var config = new QueryableConfig<Customer>().AllowFilter(c => c.Name);

        config.IsKnownField("Balance").Should().BeTrue();
        config.IsKnownField("NotAField").Should().BeFalse();
    }

    [Fact]
    public void Allow_registers_field_for_filter_sort_and_select_in_one_call()
    {
        var config = new QueryableConfig<Customer>().Allow(c => c.Balance);

        config.IsFilterAllowed("Balance", FilterOperator.GreaterThan).Should().BeTrue();
        config.IsSortAllowed("Balance").Should().BeTrue();
        config.IsSelectAllowed("Balance").Should().BeTrue();
    }
}
