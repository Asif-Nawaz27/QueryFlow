namespace QueryFlow.EFCore.Tests.TestSupport;

public sealed class Author
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<Book> Books { get; set; } = [];
}
