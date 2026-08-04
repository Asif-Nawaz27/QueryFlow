using Microsoft.EntityFrameworkCore;

namespace QueryFlow.EFCore.Tests.TestSupport;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>().HasMany(a => a.Books).WithOne(b => b.Author).HasForeignKey(b => b.AuthorId);

        // SQLite has no native decimal type; map to double so ORDER BY/comparisons on Price translate.
        modelBuilder.Entity<Book>().Property(b => b.Price).HasConversion<double>();
    }
}
