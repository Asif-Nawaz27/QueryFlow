using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace QueryFlow.EFCore.Tests.TestSupport;

/// <summary>
/// Creates a fresh, seeded <see cref="LibraryDbContext"/> backed by a real (if ephemeral) SQLite
/// database. Using an actual relational provider — rather than EF Core's InMemory provider,
/// which is more lenient about query shapes it will execute — is what makes these tests a
/// meaningful check that QueryFlow's expression trees are genuinely SQL-translatable.
/// </summary>
public static class SqliteDbContextFactory
{
    public static LibraryDbContext CreateSeeded()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new LibraryDbContext(options);
        context.Database.EnsureCreated();

        var austen = new Author { Name = "Jane Austen" };
        var orwell = new Author { Name = "George Orwell" };
        context.Authors.AddRange(austen, orwell);

        context.Books.AddRange(
            new Book { Title = "Pride and Prejudice", Genre = "Romance", Price = 9.99m, Stock = 12, PublishedAt = new DateTime(1813, 1, 28), Author = austen },
            new Book { Title = "Emma", Genre = "Romance", Price = 11.50m, Stock = 5, PublishedAt = new DateTime(1815, 12, 23), Author = austen },
            new Book { Title = "1984", Genre = "Dystopian", Price = 8.99m, Stock = 0, PublishedAt = new DateTime(1949, 6, 8), Author = orwell },
            new Book { Title = "Animal Farm", Genre = "Dystopian", Price = 6.99m, Stock = 20, PublishedAt = new DateTime(1945, 8, 17), Author = orwell });

        context.SaveChanges();
        return context;
    }
}
