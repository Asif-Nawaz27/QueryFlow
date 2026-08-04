using BenchmarkDotNet.Attributes;
using QueryFlow.Abstractions.Models;
using QueryFlow.Benchmarks.Models;
using QueryFlow.Core;

namespace QueryFlow.Benchmarks;

[MemoryDiagnoser]
public class PaginationBenchmarks
{
    private List<BenchmarkOrder> _orders = [];

    [Params(100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup() => _orders = OrderDataGenerator.Generate(RowCount);

    [Benchmark(Baseline = true)]
    public int Offset_pagination_deep_page() =>
        _orders.AsQueryable()
            .Sort([new SortField("Id")])
            .Paginate(new QueryRequest { Page = 2000, PageSize = 25 })
            .Items.Count;

    [Benchmark]
    public int Cursor_pagination_first_page() =>
        _orders.AsQueryable()
            .CursorPaginate(new QueryRequest { PageSize = 25, Sort = [new SortField("Id")] })
            .Items.Count;

    [Benchmark]
    public int Cursor_pagination_walk_to_deep_page()
    {
        QueryRequest request = new() { PageSize = 25, Sort = [new SortField("Id")] };
        CursorPagedResult<BenchmarkOrder>? page = null;

        for (var i = 0; i < 2000; i++)
        {
            page = _orders.AsQueryable().CursorPaginate(request);
            if (!page.HasNext)
            {
                break;
            }

            request = request with { Cursor = page.NextCursor };
        }

        return page?.Items.Count ?? 0;
    }
}
