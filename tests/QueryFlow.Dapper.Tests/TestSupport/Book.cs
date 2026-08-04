namespace QueryFlow.Dapper.Tests.TestSupport;

public sealed class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }
}
