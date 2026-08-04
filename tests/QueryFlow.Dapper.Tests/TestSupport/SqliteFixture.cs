using global::Dapper;
using Microsoft.Data.Sqlite;

namespace QueryFlow.Dapper.Tests.TestSupport;

public static class SqliteFixture
{
    public static SqliteConnection CreateSeeded()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        connection.Execute("""
            CREATE TABLE Books (
                Id INTEGER PRIMARY KEY,
                Title TEXT NOT NULL,
                Genre TEXT NOT NULL,
                Price REAL NOT NULL,
                Stock INTEGER NOT NULL
            );
            """);

        connection.Execute(
            "INSERT INTO Books (Id, Title, Genre, Price, Stock) VALUES (@Id, @Title, @Genre, @Price, @Stock)",
            new[]
            {
                new Book { Id = 1, Title = "Pride and Prejudice", Genre = "Romance", Price = 9.99m, Stock = 12 },
                new Book { Id = 2, Title = "Emma", Genre = "Romance", Price = 11.50m, Stock = 5 },
                new Book { Id = 3, Title = "1984", Genre = "Dystopian", Price = 8.99m, Stock = 0 },
                new Book { Id = 4, Title = "Animal Farm", Genre = "Dystopian", Price = 6.99m, Stock = 20 },
            });

        return connection;
    }
}
