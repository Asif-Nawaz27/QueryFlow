# Quick Start

This walks through adding QueryFlow to a `Book` endpoint backed by EF Core, from zero to filtering/sorting/searching/paging. The complete, runnable version of this is [`samples/QueryFlow.Samples.WebApi`](../samples/QueryFlow.Samples.WebApi).

## 1. Install packages

```bash
dotnet add package QueryFlow.Core
dotnet add package QueryFlow.EFCore
dotnet add package QueryFlow.AspNetCore
```

## 2. Declare an allowlist

```csharp
using QueryFlow.Validation;
using QueryFlow.Abstractions.Enums;

public static class BookQueries
{
    public static readonly QueryableConfig<Book> Config = new QueryableConfig<Book>()
        .AllowFilter(b => b.Title, FilterOperator.Contains, FilterOperator.Equals)
        .AllowFilter(b => b.Genre, FilterOperator.Equals, FilterOperator.In)
        .AllowFilter(b => b.Price, FilterOperator.GreaterThan, FilterOperator.LessThan, FilterOperator.Between)
        .AllowSort(b => b.Title)
        .AllowSort(b => b.Price)
        .Searchable(b => b.Title)
        .Searchable(b => b.Description)
        .AllowInclude("Author");
}
```

## 3. Register QueryFlow

```csharp
builder.Services.AddQueryFlow(options =>
{
    options.DefaultPageSize = 10;
    options.MaxPageSize = 50;
});
```

## 4. Map an endpoint

```csharp
app.MapGet("/books", async (QueryRequestParameter q, LibraryDbContext db) =>
{
    var request = q.Value;
    var query = db.Books.ApplyQuery(request, BookQueries.Config); // Filter + Search + Sort + Include
    var page = await query.PaginateAsync(request);
    return Results.Ok(page);
});
```

## 5. Query it

```bash
curl "https://localhost:5001/books?filter=genre eq Romance and price lt 15&sort=-price&pageSize=5"
```

```json
{
  "items": [
    { "id": 2, "title": "Emma", "genre": "Romance", "price": 11.50, "...": "..." },
    { "id": 1, "title": "Pride and Prejudice", "genre": "Romance", "price": 9.99, "...": "..." }
  ],
  "page": 1,
  "pageSize": 5,
  "totalCount": 2,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## Adding field selection

```csharp
app.MapGet("/books/compact", async (QueryRequestParameter q, LibraryDbContext db) =>
{
    var request = q.Value; // ?fields=id,title,price
    var query = db.Books.ApplyQuery(request, BookQueries.Config);
    var projected = query.SelectFields(request.Fields, BookQueries.Config);
    var page = await projected.PaginateAsync(request);
    return Results.Ok(new
    {
        page.Page, page.PageSize, page.TotalCount,
        Items = page.Items.Select(i => i.ToFieldDictionary())
    });
});
```

## Adding cursor pagination

```csharp
app.MapGet("/books/cursor", async (QueryRequestParameter q, LibraryDbContext db) =>
{
    var request = q.Value; // ?sort=id&pageSize=25[&cursor=...]
    var page = await db.Books.ApplyQuery(request, BookQueries.Config).CursorPaginateAsync(request);
    return Results.Ok(page); // { items, nextCursor, previousCursor, hasNext, hasPrevious }
});
```

Cursor pagination requires at least one `Sort` field (it's what makes the keyset seek deterministic) — pass `sort=id` or any other stable ordering.

## Next steps

- [Architecture Overview](architecture.md) — how the packages fit together.
- [API Reference](api-reference.md) — every extension method and type.
- [Migration Guide](migration-guide.md) — coming from hand-rolled query params or OData.
- [FAQ](faq.md)
