using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Exceptions;
using QueryFlow.Abstractions.Models;
using QueryFlow.Abstractions.Options;
using QueryFlow.Validation.Tests.TestSupport;

namespace QueryFlow.Validation.Tests;

public class QueryRequestValidatorTests
{
    [Fact]
    public void Valid_request_does_not_throw()
    {
        var request = new QueryRequest
        {
            Filters = [new FilterCondition("Name", FilterOperator.Contains, "a")],
            Sort = [new SortField("Balance")],
            PageSize = 25
        };

        var act = () => QueryRequestValidator.Validate(request, QueryableConfig<Customer>.Default, new QueryFlowOptions());

        act.Should().NotThrow();
    }

    [Fact]
    public void Invalid_field_in_filters_throws_InvalidFieldException()
    {
        var request = new QueryRequest { Filters = [new FilterCondition("Bogus", FilterOperator.Equals, "x")] };

        var act = () => QueryRequestValidator.Validate(request, QueryableConfig<Customer>.Default, new QueryFlowOptions());

        act.Should().Throw<InvalidFieldException>();
    }

    [Fact]
    public void Disallowed_operator_throws_InvalidOperatorException()
    {
        var config = new QueryableConfig<Customer>().AllowFilter(c => c.Name, FilterOperator.Equals);
        var request = new QueryRequest { Filters = [new FilterCondition("Name", FilterOperator.Contains, "a")] };

        var act = () => QueryRequestValidator.Validate(request, config, new QueryFlowOptions());

        act.Should().Throw<InvalidOperatorException>();
    }

    [Fact]
    public void PageSize_over_max_throws_PageSizeExceededException()
    {
        var request = new QueryRequest { PageSize = 1000 };
        var options = new QueryFlowOptions { MaxPageSize = 100 };

        var act = () => QueryRequestValidator.Validate(request, QueryableConfig<Customer>.Default, options);

        act.Should().Throw<PageSizeExceededException>();
    }

    [Fact]
    public void Too_many_filter_conditions_throws_TooManyClausesException()
    {
        var conditions = Enumerable.Range(0, 30)
            .Select(_ => new FilterCondition("Name", FilterOperator.Equals, "x"))
            .ToArray();
        var request = new QueryRequest { Filters = conditions };
        var options = new QueryFlowOptions { MaxFilterConditions = 25 };

        var act = () => QueryRequestValidator.Validate(request, QueryableConfig<Customer>.Default, options);

        act.Should().Throw<TooManyClausesException>();
    }

    [Fact]
    public void Too_many_sort_fields_throws_TooManyClausesException()
    {
        var sort = Enumerable.Range(0, 10).Select(_ => new SortField("Name")).ToArray();
        var request = new QueryRequest { Sort = sort };
        var options = new QueryFlowOptions { MaxSortFields = 5 };

        var act = () => QueryRequestValidator.Validate(request, QueryableConfig<Customer>.Default, options);

        act.Should().Throw<TooManyClausesException>();
    }

    [Fact]
    public void Unknown_sort_field_throws_InvalidFieldException()
    {
        var request = new QueryRequest { Sort = [new SortField("Bogus")] };

        var act = () => QueryRequestValidator.Validate(request, QueryableConfig<Customer>.Default, new QueryFlowOptions());

        act.Should().Throw<InvalidFieldException>();
    }

    [Fact]
    public void Unknown_select_field_throws_InvalidFieldException()
    {
        var request = new QueryRequest { Fields = ["Bogus"] };

        var act = () => QueryRequestValidator.Validate(request, QueryableConfig<Customer>.Default, new QueryFlowOptions());

        act.Should().Throw<InvalidFieldException>();
    }

    [Fact]
    public void Include_not_in_allowlist_throws_InvalidFieldException()
    {
        var config = new QueryableConfig<Customer>().AllowInclude("Orders");
        var request = new QueryRequest { Includes = ["Payments"] };

        var act = () => QueryRequestValidator.Validate(request, config, new QueryFlowOptions());

        act.Should().Throw<InvalidFieldException>();
    }

    [Fact]
    public void Nested_FilterGroup_is_validated_recursively()
    {
        var request = new QueryRequest
        {
            FilterGroup = new FilterGroup(
                LogicalOperator.And,
                Groups: [new FilterGroup(LogicalOperator.Or, Conditions: [new FilterCondition("Bogus", FilterOperator.Equals, "x")])])
        };

        var act = () => QueryRequestValidator.Validate(request, QueryableConfig<Customer>.Default, new QueryFlowOptions());

        act.Should().Throw<InvalidFieldException>();
    }

    [Fact]
    public void ClampPageSize_clamps_to_max_and_defaults_when_unspecified()
    {
        var options = new QueryFlowOptions { DefaultPageSize = 25, MaxPageSize = 50 };

        QueryRequestValidator.ClampPageSize(null, options).Should().Be(25);
        QueryRequestValidator.ClampPageSize(1000, options).Should().Be(50);
        QueryRequestValidator.ClampPageSize(10, options).Should().Be(10);
    }
}
