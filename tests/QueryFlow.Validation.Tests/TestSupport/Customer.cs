namespace QueryFlow.Validation.Tests.TestSupport;

public sealed class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public decimal Balance { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
