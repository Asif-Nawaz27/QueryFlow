namespace QueryFlow.Samples.WebApi.Models;

public sealed class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime PublishedAt { get; set; }

    public string? Description { get; set; }

    public int AuthorId { get; set; }

    public Author? Author { get; set; }
}
