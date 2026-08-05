# FAQ

**Does QueryFlow work without EF Core?**
Yes. `QueryFlow.Core` operates on any `IQueryable<T>`, including plain in-memory `List<T>.AsQueryable()`. `QueryFlow.EFCore` only adds `Include()` support and an `ApplyQuery()` convenience wrapper — everything else works the same either way. For Dapper (which isn't `IQueryable`-based), use `QueryFlow.Dapper`'s `DapperQueryBuilder<T>` instead.

**Do I have to configure a `QueryableConfig<T>`?**
No — an unconfigured config allows every public top-level property with type-appropriate operators. You only configure it to *restrict* what's queryable. See [Installation](installation.md#minimal-setup).

**How does field selection avoid over-fetching with EF Core?**
`SelectFields` projects onto a runtime-emitted CLR type (one property per requested field) via `System.Reflection.Emit`, not a dictionary. EF Core translates a `MemberInit` projection against a real type into a SQL `SELECT` of just those columns, the same way projecting onto an anonymous type does. See [Architecture](architecture.md#field-selection-without-a-hard-coded-dto).

**Is cursor pagination just base64-encoded offset?**
No — it's genuine keyset ("seek method") pagination. The cursor encodes the sort-key values of the last row seen; the next page is fetched via `WHERE (key1, key2, ...) > (v1, v2, ...)`, not `OFFSET n`. This stays correct (no skipped/duplicated rows) even if rows are inserted or deleted while a client is paging, which offset pagination does not guarantee. Cursors are HMAC-signed so clients can't forge one.

**Can I mix offset and cursor pagination on the same endpoint?**
Yes, they're independent methods (`PaginateAsync` vs `CursorPaginateAsync`) — call whichever fits the request, e.g. based on whether `request.Cursor` is set. See `samples/QueryFlow.Samples.WebApi` for an example splitting them into `/books` (offset) and `/books/cursor` (keyset).

**Why does `CursorPaginateAsync` require `Sort`?**
Keyset pagination needs a deterministic total order to seek through — without one, "the next 25 rows after this one" isn't well-defined. Pass at least one sort field (a unique key like `Id` is the safest single choice).

**What happens with an invalid field or operator?**
`QueryableConfig<T>` checks throw `InvalidFieldException` or `InvalidOperatorException` (both `QueryFlowException`). With `QueryFlow.AspNetCore`'s `UseQueryFlowExceptionHandling()` middleware, these become `400 Bad Request` `ProblemDetails` responses automatically instead of an unhandled `500`.

**How do I cap page size / prevent expensive filter chains?**
`QueryFlowOptions.MaxPageSize`, `MaxFilterConditions`, `MaxSortFields`, and `MaxIncludeDepth` are enforced by `QueryRequestValidator` (and by `Paginate`/`CursorPaginate`, which clamp/validate page size internally). Configure them via `services.AddQueryFlow(options => ...)`.

**Are cursors portable across app restarts / multiple instances?**
Only if you set `QueryFlowOptions.CursorSigningKey` explicitly. Without it, each process generates a random signing key at startup — cursors from one process instance won't validate against another. Any deployment with more than one instance (or restarts mid-session) should set this explicitly.

**Does the query-string `filter=` DSL support parentheses / arbitrary nesting?**
No — it's intentionally a flat `field op value [and|or field op value ...]` grammar. For nested AND/OR logic, POST a JSON body with a `filterGroup` tree (`FilterGroup`), which `Filter()` accepts directly and *does* support arbitrary nesting.

**Can I use QueryFlow with Postgres/MySQL/SQLite, not just SQL Server?**
Yes for `QueryFlow.EFCore` (whatever EF Core provider you use). For `QueryFlow.Dapper`, pass the matching `DatabaseProvider` to `DapperQueryBuilder<T>.Build(...)` to get the right paging syntax (`OFFSET/FETCH` for SQL Server, `LIMIT/OFFSET` for the rest).

**How is this different from OData or `System.Linq.Dynamic.Core`?**
See the [Migration Guide](migration-guide.md#from-odata-microsoftaspnetcoreodata) — short version: QueryFlow trades the full expressiveness of those tools for an explicit, secure-by-default allowlist and a much smaller API surface.
