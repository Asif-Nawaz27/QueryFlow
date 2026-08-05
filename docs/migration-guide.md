# Migration Guide

## From hand-rolled query parameters

A typical ad-hoc implementation looks like:

```csharp
app.MapGet("/books", async (string? genre, decimal? minPrice, string? sort, int page, int pageSize, AppDbContext db) =>
{
    var query = db.Books.AsQueryable();
    if (genre is not null) query = query.Where(b => b.Genre == genre);
    if (minPrice is not null) query = query.Where(b => b.Price >= minPrice);
    query = sort switch { "price" => query.OrderBy(b => b.Price), "-price" => query.OrderByDescending(b => b.Price), _ => query };
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    return items;
});
```

Every new filterable field means another `if` branch and another manual `Where`. The QueryFlow equivalent generalizes over *any* allowlisted field without new code per field:

```csharp
public static readonly QueryableConfig<Book> Config = new QueryableConfig<Book>()
    .AllowFilter(b => b.Genre, FilterOperator.Equals)
    .AllowFilter(b => b.Price, FilterOperator.GreaterThanOrEqual)
    .AllowSort(b => b.Price);

app.MapGet("/books", async (QueryRequestParameter q, AppDbContext db) =>
{
    var page = await db.Books.ApplyQuery(q.Value, Config).PaginateAsync(q.Value);
    return Results.Ok(page);
});
```

Query string changes from `?genre=Romance&minPrice=10&sort=-price` to `?filter=genre eq Romance and price gte 10&sort=-price` — functionally equivalent, but every new allowlisted field is a one-line config change, not a new parameter/branch/test.

## From OData (`Microsoft.AspNetCore.OData`)

OData's `$filter`/`$orderby`/`$select`/`$top`/`$skip` map conceptually to QueryFlow's `filter`/`sort`/`fields`/`page`/`pageSize`, but OData exposes its full query language (including arbitrary property access) unless you explicitly restrict it with `[EnableQuery]` attributes and edm model configuration. QueryFlow's allowlist is the default, not an opt-in.

| OData | QueryFlow |
|---|---|
| `$filter=Age gt 18` | `filter=age gt 18` |
| `$orderby=CreatedAt desc` | `sort=-createdAt` |
| `$select=id,name` | `fields=id,name` |
| `$top=25&$skip=50` | `page=3&pageSize=25` |
| `$expand=Author` | `include=author` |
| `$count=true` | `AggregateAsync([new AggregateRequest(AggregateFunction.Count)])` |

QueryFlow does not implement the OData wire protocol or `$filter` expression grammar (e.g. `substringof`, `any`/`all` lambdas) — if you need OData clients specifically, keep `Microsoft.AspNetCore.OData`. QueryFlow is for teams who want filtering/sorting/paging without adopting the full OData spec.

## From `System.Linq.Dynamic.Core`

Dynamic LINQ evaluates a raw expression string (`"Age > 18 && Country == \"US\""`) against `T`. It's flexible, but the security model is "make sure the string is safe," which requires careful scoping of what properties/methods are reachable from the parser. QueryFlow instead takes structured input (`FilterCondition` records) validated field-by-field against an explicit allowlist — there's no expression string to sandbox in the first place.

If you have existing Dynamic LINQ query strings from clients, you'd need a translation layer that parses them into `FilterCondition`s; QueryFlow doesn't do this for you today (see the [FAQ](faq.md) for why the query-string DSL is intentionally simpler).

## Adding QueryFlow to an existing EF Core repository/service layer

QueryFlow's extension methods compose into any existing `IQueryable<T>` pipeline — you don't need to restructure a repository pattern:

```csharp
public async Task<PagedResult<Order>> GetOrdersAsync(QueryRequest request, CancellationToken ct)
{
    IQueryable<Order> query = _context.Orders.Where(o => !o.IsDeleted); // your existing base query
    query = query.ApplyQuery(request, OrderQueries.Config);              // QueryFlow takes over from here
    return await query.PaginateAsync(request, cancellationToken: ct);
}
```

## Version compatibility

QueryFlow targets .NET 8, 9, and 10. There are no QueryFlow-specific breaking changes to migrate yet (pre-1.0) — see [CHANGELOG.md](../CHANGELOG.md) for release notes going forward.
