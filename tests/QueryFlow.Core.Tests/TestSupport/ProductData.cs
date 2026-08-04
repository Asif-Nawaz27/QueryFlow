namespace QueryFlow.Core.Tests.TestSupport;

public static class ProductData
{
    public static IQueryable<Product> Sample() => new List<Product>
    {
        new() { Id = 1, Name = "Widget", Category = "Hardware", Price = 9.99m, Stock = 100, IsActive = true, CreatedAt = new DateTime(2024, 1, 1), Description = "A basic widget" },
        new() { Id = 2, Name = "Gadget", Category = "Electronics", Price = 29.99m, Stock = 50, IsActive = true, CreatedAt = new DateTime(2024, 2, 1), Description = "A fancy gadget" },
        new() { Id = 3, Name = "Gizmo", Category = "Electronics", Price = 49.99m, Stock = 0, IsActive = false, CreatedAt = new DateTime(2024, 3, 1), Description = null },
        new() { Id = 4, Name = "Doohickey", Category = "Hardware", Price = 15.50m, Stock = 200, IsActive = true, CreatedAt = new DateTime(2024, 4, 1), Description = "Assorted doohickey" },
        new() { Id = 5, Name = "Thingamajig", Category = "Misc", Price = 5.00m, Stock = 10, IsActive = true, CreatedAt = new DateTime(2024, 5, 1), Description = "Uncategorized item" },
    }.AsQueryable();
}
