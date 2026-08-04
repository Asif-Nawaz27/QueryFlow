using BenchmarkDotNet.Attributes;
using QueryFlow.Benchmarks.Models;
using QueryFlow.Core;

namespace QueryFlow.Benchmarks;

[MemoryDiagnoser]
public class SelectFieldsBenchmarks
{
    private List<BenchmarkOrder> _orders = [];

    [Params(100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup() => _orders = OrderDataGenerator.Generate(RowCount);

    [Benchmark(Baseline = true)]
    public int Full_entity_materialization() =>
        _orders.AsQueryable().ToList().Count;

    [Benchmark]
    public int QueryFlow_SelectFields_projection() =>
        _orders.AsQueryable().SelectFields(["Id", "CustomerName", "Total"]).ToList().Count;
}
