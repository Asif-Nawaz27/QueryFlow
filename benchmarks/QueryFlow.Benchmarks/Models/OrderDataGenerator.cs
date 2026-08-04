namespace QueryFlow.Benchmarks.Models;

public static class OrderDataGenerator
{
    private static readonly string[] Statuses = ["Pending", "Shipped", "Delivered", "Cancelled"];
    private static readonly string[] Names = ["Alice", "Bob", "Carol", "Dave", "Erin", "Frank", "Grace", "Heidi"];

    public static List<BenchmarkOrder> Generate(int count)
    {
        var random = new Random(42);
        var orders = new List<BenchmarkOrder>(count);

        for (var i = 0; i < count; i++)
        {
            orders.Add(new BenchmarkOrder
            {
                Id = i,
                CustomerName = Names[random.Next(Names.Length)],
                Status = Statuses[random.Next(Statuses.Length)],
                Total = (decimal)Math.Round(random.NextDouble() * 1000, 2),
                Quantity = random.Next(1, 20),
                CreatedAt = new DateTime(2020, 1, 1).AddMinutes(random.Next(0, 3_000_000)),
                IsPriority = random.Next(2) == 0
            });
        }

        return orders;
    }
}
