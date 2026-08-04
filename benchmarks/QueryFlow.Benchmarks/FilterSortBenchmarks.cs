using BenchmarkDotNet.Attributes;
using QueryFlow.Abstractions.Enums;
using QueryFlow.Abstractions.Models;
using QueryFlow.Benchmarks.Models;
using QueryFlow.Core;

namespace QueryFlow.Benchmarks;

[MemoryDiagnoser]
public class FilterSortBenchmarks
{
    private List<BenchmarkOrder> _orders = [];

    [Params(1_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup() => _orders = OrderDataGenerator.Generate(RowCount);

    [Benchmark(Baseline = true)]
    public int Linq_manual_filter() =>
        _orders.AsQueryable().Where(o => o.Status == "Shipped" && o.Total > 100).Count();

    [Benchmark]
    public int QueryFlow_single_filter() =>
        _orders.AsQueryable()
            .Filter([new FilterCondition("Status", FilterOperator.Equals, "Shipped")])
            .Count();

    [Benchmark]
    public int QueryFlow_multi_condition_filter() =>
        _orders.AsQueryable()
            .Filter(
            [
                new FilterCondition("Status", FilterOperator.Equals, "Shipped"),
                new FilterCondition("Total", FilterOperator.GreaterThan, 100m)
            ])
            .Count();

    [Benchmark]
    public int QueryFlow_multi_column_sort() =>
        _orders.AsQueryable()
            .Sort([new SortField("Status"), new SortField("Total", SortDirection.Descending)])
            .Count();

    [Benchmark]
    public int QueryFlow_filter_then_sort_then_paginate() =>
        _orders.AsQueryable()
            .Filter([new FilterCondition("IsPriority", FilterOperator.Equals, true)])
            .Sort([new SortField("CreatedAt", SortDirection.Descending)])
            .Paginate(new QueryRequest { Page = 1, PageSize = 25 })
            .Items.Count;
}
