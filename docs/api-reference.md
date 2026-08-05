# API Reference

This is a reference of the public surface, organized by package. For a task-oriented walkthrough, start with the [Quick Start](quick-start.md).

## QueryFlow.Abstractions

### Models

| Type | Purpose |
|---|---|
| `FilterCondition(Field, Operator, Value?, Values?, Connector)` | One filter clause. `Values` is used for `Between`/`In`/`NotIn`; `Connector` says how it joins the *next* condition in a flat list. |
| `FilterGroup(Operator, Conditions?, Groups?)` | Nested AND/OR tree of conditions and sub-groups, for logic the flat DSL can't express. |
| `SortField(Field, Direction)` | One sort key. `SortField.Parse("-CreatedDate")` parses the `-`/`+`-prefixed query-string token form. |
| `QueryRequest` | The immutable, transport-agnostic request: `Filters`, `FilterGroup`, `Search`, `Sort`, `Fields`, `Includes`, `Aggregates`, `Page`, `PageSize`, `Cursor`. |
| `PagedResult<T>` | `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`, `HasNextPage`, `HasPreviousPage`. |
| `CursorPagedResult<T>` | `Items`, `PageSize`, `NextCursor`, `PreviousCursor`, `HasNext`, `HasPrevious`. |
| `AggregateRequest(Function, Field?)` / `AggregateResult(Function, Field?, Value)` | Aggregate computation request/result. |

### Enums

`FilterOperator`, `SortDirection`, `LogicalOperator` (And/Or), `AggregateFunction` (Count/Sum/Average/Max/Min).

### Options

`QueryFlowOptions` — `DefaultPageSize`, `MaxPageSize`, `MaxFilterConditions`, `MaxSortFields`, `MaxIncludeDepth`, `CaseInsensitiveSearch`, `CursorSigningKey`.

### Exceptions

All derive from `QueryFlowException`: `InvalidFieldException`, `InvalidOperatorException`, `PageSizeExceededException`, `TooManyClausesException`, `InvalidCursorException`.

## QueryFlow.Validation

### `QueryableConfig<T>`

The allowlist builder. Unconfigured = wide open (every public property, type-appropriate operators). Calling any `Allow*`/`Searchable` overload switches *that capability* into strict allowlist mode.

```csharp
new QueryableConfig<Book>()
    .AllowFilter(b => b.Price, FilterOperator.GreaterThan, FilterOperator.Between)
    .AllowFilter("Genre", FilterOperator.Equals)       // string-field overload also available
    .AllowSort(b => b.Price)
    .Searchable(b => b.Title)                          // string properties only
    .AllowSelect(b => b.Title)
    .AllowInclude("Author")                             // navigation property name
    .Allow(b => b.Id);                                  // filter + sort + select in one call
```

Query methods: `IsFilterAllowed`, `IsSortAllowed`, `IsSearchable`, `IsSelectAllowed`, `IsIncludeAllowed`, `IsKnownField`. `QueryableConfig<T>.ResolveIncludePath(path)` normalizes a case-insensitively-matched include path to the exact-case CLR navigation path EF Core's `Include(string)` requires.

### `QueryRequestValidator`

`Validate<T>(request, config, options)` runs every check (field/operator allowlist, page size, clause counts) and throws the first violation. `ClampPageSize(requested, options)` clamps instead of throwing.

## QueryFlow.Core

All are extension methods on `IQueryable<T>` (or `IQueryable<object>` where the element type changes).

| Method | Signature (abbreviated) | Notes |
|---|---|---|
| `Filter` | `Filter(IReadOnlyList<FilterCondition>\|FilterGroup\|QueryRequest, config?)` | |
| `Search` | `Search(string? term, config)` | `config` required — matches whichever fields are `Searchable`. |
| `Sort` | `Sort(IReadOnlyList<SortField>\|string, config?)` | String overload parses `"Field,-Field2"` tokens. |
| `SelectFields` | `SelectFields(IReadOnlyList<string> fields, config?) : IQueryable<object>` | Real column-level SQL projection; `.ToFieldDictionary()` converts a materialized item to JSON-friendly form. |
| `Paginate` / `PaginateAsync` | `(QueryRequest, options?) : PagedResult<T>` | Offset pagination. Async uses real EF Core async I/O when available. |
| `CursorPaginate` / `CursorPaginateAsync` | `(QueryRequest, options?) : CursorPagedResult<T>` | Keyset pagination; `request.Sort` must be non-empty. |
| `AggregateAsync` | `(IReadOnlyList<AggregateRequest>, config?) : IReadOnlyList<AggregateResult>` | |

## QueryFlow.EFCore

| Method | Notes |
|---|---|
| `Include(IReadOnlyList<string> paths, config?)` | Validated, case-normalized eager loading (supports dotted paths). |
| `ApplyQuery(QueryRequest, config?)` | One call: `Filter` + `Search` + `Sort` + `Include`. |

## QueryFlow.Dapper

`DapperQueryBuilder<T>(config?, columnMap?)` builds parameterized SQL fragments:

```csharp
var builder = new DapperQueryBuilder<Book>(BookQueries.Config);
var query = builder.Build(request, DatabaseProvider.PostgreSql);
// query.WhereSql, query.OrderBySql, query.PagingSql, query.Parameters (Dapper.DynamicParameters)

var rows = await connection.QueryAsync<Book>(
    $"SELECT * FROM books {query.WhereSql} {query.OrderBySql} {query.PagingSql}",
    query.Parameters);
```

`DatabaseProvider` selects paging syntax: `SqlServer` (`OFFSET ... FETCH NEXT`) vs `PostgreSql`/`MySql`/`Sqlite` (`LIMIT ... OFFSET`). `columnMap` translates a logical field name to a differently-named physical column.

## QueryFlow.AspNetCore

| Type/Method | Notes |
|---|---|
| `HttpRequest.ToQueryRequest()` | Parses `filter`/`sort`/`search`/`fields`/`include`/`page`/`pageSize`/`cursor` from the query string. |
| `QueryRequestParameter` | Minimal-API `BindAsync` wrapper; declare it as a handler parameter for automatic binding. Implicitly converts to `QueryRequest`. |
| `FilterQueryStringParser.Parse(string)` | The `?filter=age gt 18 and city eq "NY"` DSL parser, used internally by `ToQueryRequest`. |
| `services.AddQueryFlow(configure?)` | Registers `QueryFlowOptions` as a singleton. |
| `app.UseQueryFlowExceptionHandling()` | Middleware: `QueryFlowException` → `400` `ProblemDetails`. |

### Filter query-string DSL

```
field operator value [and|or field operator value ...]
```

Operators: `eq ne gt gte lt lte between in nin contains startswith endswith isnull isnotnull`. Values may be quoted (`"New York"`), bare words/numbers/booleans, or comma-separated lists (`between`/`in`/`nin`). For richer nesting, POST a JSON body with `filters` or `filterGroup` instead.

## QueryFlow.OpenApi

`services.AddSwaggerGen(c => c.AddQueryFlow())` registers an `IOperationFilter` that documents the standard query parameters on any endpoint binding `QueryRequest`/`QueryRequestParameter`.
