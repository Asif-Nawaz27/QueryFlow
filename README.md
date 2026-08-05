# QueryFlow

**The developer-friendly Dynamic API Query Toolkit for .NET.**

Add filtering, searching, sorting, field selection, pagination, and query optimization to any REST API — with a handful of allowlist declarations and one line per request.

```csharp
var result = await db.Users
    .Filter(request)
    .Search(request.Search, config)
    .Sort(request.Sort)
    .PaginateAsync(request);
```

```
GET /users?filter=age gt 18 and country eq "US"&sort=-createdAt&search=jane&page=1&pageSize=25
```

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%2C%209%2C%2010-512BD4)](#requirements)
[![CI](https://github.com/Asif-Nawaz27/QueryFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/Asif-Nawaz27/QueryFlow/actions/workflows/ci.yml)

---

## Why QueryFlow

Every API ends up reinventing "let the client filter/sort/page the list" — usually as a pile of `if (request.Sort == "...")` branches that quietly become a SQL injection or over-fetching risk. QueryFlow gives you that capability as a small set of composable `IQueryable<T>` extension methods, backed by an explicit **allowlist** so clients can only touch fields and operators you've approved.

- **ORM-independent core.** `QueryFlow.Core` works against any `IQueryable<T>` — EF Core, an in-memory `List<T>.AsQueryable()`, anything. `QueryFlow.EFCore` adds eager-loading; `QueryFlow.Dapper` adds safe, parameterized SQL fragment building for raw-SQL codebases.
- **Secure by default.** Every field name and operator is checked against a `QueryableConfig<T>` allowlist before it touches an expression tree or a SQL string. Page sizes and clause counts are capped. Nothing gets string-concatenated into SQL.
- **Real cursor pagination.** Not "base64(offset)" — genuine keyset pagination with HMAC-signed, tamper-evident cursors that stay correct under concurrent inserts/deletes, with bidirectional (next/previous) support.
- **Performant.** Reflection (property paths, generic method resolution) is cached; expression trees stay in a shape LINQ providers can translate to SQL, including field selection, which projects onto a runtime-emitted type so EF Core only selects the columns you asked for — not the whole row.
- **Minimal setup.** An unconfigured `QueryableConfig<T>` allows every public property with type-appropriate operators. You only write an allowlist when you want to *restrict* something.

## Requirements

.NET 8.0, 9.0, or 10.0.

## Packages

| Package | What it's for |
|---|---|
| `QueryFlow.Abstractions` | Request/response models, enums, exceptions, options — no dependencies. |
| `QueryFlow.Expressions` | Expression-tree builders for filter/sort/search/select/cursor-seek/aggregate, with cached reflection. |
| `QueryFlow.Validation` | `QueryableConfig<T>` allowlist builder + request validator. |
| `QueryFlow.Core` | The main developer-facing API: `Filter`, `Search`, `Sort`, `SelectFields`, `Paginate(Async)`, `CursorPaginate(Async)`, `AggregateAsync`. |
| `QueryFlow.EFCore` | `Include()` eager loading + `ApplyQuery()` one-call composition for EF Core. |
| `QueryFlow.Dapper` | `DapperQueryBuilder<T>` — safe, parameterized WHERE/ORDER BY/paging SQL fragments for raw-SQL codebases. |
| `QueryFlow.AspNetCore` | Query-string DSL parser, minimal-API model binding, DI registration, exception → `ProblemDetails` middleware. |
| `QueryFlow.OpenApi` | Swashbuckle operation filter documenting the standard query parameters. |

Install what you need:

```bash
dotnet add package QueryFlow.Core
dotnet add package QueryFlow.EFCore        # if you're on EF Core
dotnet add package QueryFlow.Dapper        # if you're on Dapper
dotnet add package QueryFlow.AspNetCore    # query-string binding + DI
dotnet add package QueryFlow.OpenApi       # optional, Swagger docs
```

## Quick start

**1. Declare what clients are allowed to query:**

```csharp
public static class UserQueries
{
    public static readonly QueryableConfig<User> Config = new QueryableConfig<User>()
        .AllowFilter(u => u.Age, FilterOperator.GreaterThan, FilterOperator.Equals)
        .AllowFilter(u => u.Country, FilterOperator.Equals, FilterOperator.In)
        .AllowSort(u => u.CreatedAt)
        .AllowSort(u => u.Name)
        .Searchable(u => u.Name)
        .Searchable(u => u.Email);
}
```

**2. Wire up an endpoint:**

```csharp
app.MapGet("/users", async (QueryRequestParameter q, AppDbContext db) =>
{
    var request = q.Value;
    var query = db.Users.ApplyQuery(request, UserQueries.Config); // Filter + Search + Sort
    var page = await query.PaginateAsync(request);
    return Results.Ok(page);
});
```

**3. Query it:**

```
GET /users?filter=age gt 18 and country eq "US"&sort=-createdAt&search=jane&page=1&pageSize=25
```

That's it — see [`samples/QueryFlow.Samples.WebApi`](samples/QueryFlow.Samples.WebApi) for a complete, runnable example (books/authors API) covering filtering, search, sorting, field selection, both pagination styles, aggregates, and eager loading.

## Core features

### Dynamic filtering

`Equals · NotEquals · GreaterThan · GreaterThanOrEqual · LessThan · LessThanOrEqual · Between · In · NotIn · Contains · StartsWith · EndsWith · IsNull · IsNotNull`

```
GET /users?filter=age gt 18
```
```json
{ "filters": [ { "field": "Age", "operator": "GreaterThan", "value": 18 } ] }
```

Nested AND/OR groups are supported via `FilterGroup` for cases the flat query-string DSL can't express.

### Sorting

```
GET /users?sort=LastName,-CreatedDate
```

### Searching

```
GET /users?search=john
```

Matches whichever fields you registered with `.Searchable(...)`.

### Pagination — offset and cursor

```
GET /users?page=3&pageSize=25
GET /users?cursor=eyJrZXlzIjpb...&pageSize=25
```

Cursors are opaque, base64url-encoded, HMAC-signed keyset tokens — not an offset in disguise. They stay correct as rows are inserted/deleted around the page boundary, and support paging backward via `previousCursor`.

### Field selection

```
GET /users?fields=id,name,email
```

Projects down to just the requested columns — for EF Core, that's a real `SELECT id, name, email`, not a full-row fetch trimmed in memory.

### Includes (eager loading)

```
GET /orders?include=customer,address
```

### Aggregates

`Count · Sum · Average · Max · Min`, computed server-side via `AggregateAsync`.

### Security

- Field names and operators are checked against your `QueryableConfig<T>` allowlist before any expression tree or SQL string is built.
- `QueryFlowOptions.MaxPageSize` / `MaxFilterConditions` / `MaxSortFields` / `MaxIncludeDepth` cap request size.
- Dapper SQL fragments use bound parameters exclusively; identifiers are re-validated against a strict pattern before being spliced into SQL text.
- Cursor tokens are HMAC-signed — clients can't forge one to skip validation or see data outside their filtered view.

## Documentation

- [Quick Start](docs/quick-start.md)
- [Installation](docs/installation.md)
- [Architecture Overview](docs/architecture.md)
- [API Reference](docs/api-reference.md)
- [Migration Guide](docs/migration-guide.md)
- [FAQ](docs/faq.md)
- [Contributing](CONTRIBUTING.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Changelog](CHANGELOG.md)

## Benchmarks

Run `dotnet run -c Release --project benchmarks/QueryFlow.Benchmarks` for BenchmarkDotNet results on filtering, sorting, pagination and field selection at 1K/100K rows.

## Contributing

Contributions are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md). Issues labeled [`good first issue`](https://github.com/Asif-Nawaz27/QueryFlow/labels/good%20first%20issue) are a good place to start.

## Roadmap

MongoDB / Cosmos DB / Elasticsearch providers · OData compatibility · GraphQL support · Specification Pattern integration · Blazor helpers · OpenTelemetry instrumentation · query analytics · source generators for compile-time allowlists.

## License

[MIT](LICENSE)
