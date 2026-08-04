namespace QueryFlow.Expressions.Tests.TestSupport;

public sealed class Address
{
    public string City { get; set; } = string.Empty;
}

public sealed class Widget
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int? Quantity { get; set; }

    public Guid ExternalId { get; set; }

    public WidgetStatus Status { get; set; }

    public Address? Address { get; set; }
}

public enum WidgetStatus
{
    Draft,
    Active,
    Retired
}
