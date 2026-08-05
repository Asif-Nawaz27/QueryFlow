# Installation

## Requirements

- .NET 8.0, 9.0, or 10.0
- One of: Entity Framework Core (any provider — SQL Server, PostgreSQL, SQLite, MySQL, ...), Dapper, or plain `IQueryable<T>` / in-memory collections.

## Packages

Install only what you need — every package depends on `QueryFlow.Abstractions` and pulls in what it needs from there:

```bash
# Always needed: the main Filter/Search/Sort/Paginate API
dotnet add package QueryFlow.Core

# Pick your data access story
dotnet add package QueryFlow.EFCore      # EF Core: Include() + ApplyQuery()
dotnet add package QueryFlow.Dapper      # Dapper: DapperQueryBuilder<T>

# ASP.NET Core integration: query-string parsing, DI, error handling
dotnet add package QueryFlow.AspNetCore

# Optional: Swagger/OpenAPI documentation for the standard query params
dotnet add package QueryFlow.OpenApi
```

If you only need the expression-building or validation primitives directly (for example, to build your own query pipeline), you can reference `QueryFlow.Expressions` or `QueryFlow.Validation` on their own.

## Minimal setup

QueryFlow works with **zero configuration** — an unconfigured `QueryableConfig<T>` allows filtering, sorting, and selecting on every public top-level property of `T`, with whichever operators make sense for each property's CLR type (e.g. numeric fields get comparisons, string fields get `Contains`/`StartsWith`/`EndsWith`).

```csharp
var result = await db.Users.Filter(request).PaginateAsync(request);
```

Configuration is opt-in when you want to *restrict* the surface area:

```csharp
var config = new QueryableConfig<User>()
    .AllowFilter(u => u.Age, FilterOperator.GreaterThan)  // now ONLY Age/GreaterThan is allowed for filtering
    .AllowSort(u => u.CreatedAt)
    .Searchable(u => u.Name);

var result = await db.Users.Filter(request, config).PaginateAsync(request);
```

Calling any `AllowFilter`/`AllowSort`/`AllowSelect`/`AllowInclude`/`Searchable` overload switches *that specific capability* into strict allowlist mode. Capabilities you never touch stay wide open — so you can lock down filtering while leaving sorting unrestricted, for example.

## ASP.NET Core wiring

```csharp
builder.Services.AddQueryFlow(options =>
{
    options.DefaultPageSize = 25;
    options.MaxPageSize = 100;
    // options.CursorSigningKey = Convert.FromBase64String(...); // set explicitly for multi-instance deployments
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.AddQueryFlow()); // requires QueryFlow.OpenApi

var app = builder.Build();
app.UseQueryFlowExceptionHandling(); // turns QueryFlowException into 400 ProblemDetails
```

Minimal API handlers bind a `QueryRequest` automatically via `QueryRequestParameter`:

```csharp
app.MapGet("/users", async (QueryRequestParameter q, AppDbContext db) =>
{
    var page = await db.Users.ApplyQuery(q.Value, UserQueries.Config).PaginateAsync(q.Value);
    return Results.Ok(page);
});
```

For controllers, call `HttpContext.Request.ToQueryRequest()` directly.
