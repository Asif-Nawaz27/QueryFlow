using QueryFlow.Samples.WebApi.Models;

namespace QueryFlow.Samples.WebApi.Data;

public static class SeedData
{
    public static void Seed(LibraryDbContext db)
    {
        if (db.Authors.Any())
        {
            return;
        }

        var austen = new Author { Name = "Jane Austen", Country = "UK" };
        var orwell = new Author { Name = "George Orwell", Country = "UK" };
        var christie = new Author { Name = "Agatha Christie", Country = "UK" };

        db.Authors.AddRange(austen, orwell, christie);

        db.Books.AddRange(
            new Book { Title = "Pride and Prejudice", Genre = "Romance", Price = 9.99m, Stock = 12, IsAvailable = true, PublishedAt = new DateTime(1813, 1, 28), Description = "A witty look at love and marriage in Regency England.", Author = austen },
            new Book { Title = "Emma", Genre = "Romance", Price = 11.50m, Stock = 5, IsAvailable = true, PublishedAt = new DateTime(1815, 12, 23), Description = "A meddling matchmaker learns some hard lessons.", Author = austen },
            new Book { Title = "1984", Genre = "Dystopian", Price = 8.99m, Stock = 0, IsAvailable = false, PublishedAt = new DateTime(1949, 6, 8), Description = "A dystopian vision of total surveillance.", Author = orwell },
            new Book { Title = "Animal Farm", Genre = "Dystopian", Price = 6.99m, Stock = 20, IsAvailable = true, PublishedAt = new DateTime(1945, 8, 17), Description = "A political fable about power and corruption.", Author = orwell },
            new Book { Title = "Murder on the Orient Express", Genre = "Mystery", Price = 10.25m, Stock = 8, IsAvailable = true, PublishedAt = new DateTime(1934, 1, 1), Description = null, Author = christie },
            new Book { Title = "And Then There Were None", Genre = "Mystery", Price = 12.00m, Stock = 3, IsAvailable = true, PublishedAt = new DateTime(1939, 11, 6), Description = "Ten strangers, an island, and no way off.", Author = christie });

        db.SaveChanges();
    }
}
