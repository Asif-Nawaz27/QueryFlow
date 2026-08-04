namespace QueryFlow.Benchmarks.Models;

public sealed class BenchmarkOrder
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsPriority { get; set; }
}
