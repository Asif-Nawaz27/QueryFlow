using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Models;
using QueryFlow.Core.Tests.TestSupport;

namespace QueryFlow.Core.Tests;

public class AggregateTests
{
    [Fact]
    public async Task Count_returns_total_row_count()
    {
        var results = await ProductData.Sample().AggregateAsync([new AggregateRequest(AggregateFunction.Count)]);

        results.Single().Value.Should().Be(5L);
    }

    [Fact]
    public async Task Sum_computes_total_of_a_numeric_field()
    {
        var results = await ProductData.Sample().AggregateAsync([new AggregateRequest(AggregateFunction.Sum, "Price")]);

        results.Single().Value.Should().Be(110.47m);
    }

    [Fact]
    public async Task Average_computes_mean_of_a_numeric_field()
    {
        var results = await ProductData.Sample().AggregateAsync([new AggregateRequest(AggregateFunction.Average, "Stock")]);

        results.Single().Value.Should().Be(72.0);
    }

    [Fact]
    public async Task Max_and_Min_compute_bounds()
    {
        var results = await ProductData.Sample().AggregateAsync(
        [
            new AggregateRequest(AggregateFunction.Max, "Price"),
            new AggregateRequest(AggregateFunction.Min, "Price")
        ]);

        results[0].Value.Should().Be(49.99m);
        results[1].Value.Should().Be(5.00m);
    }
}
