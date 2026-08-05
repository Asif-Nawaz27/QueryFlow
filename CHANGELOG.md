# Changelog

All notable changes to this project are documented here. This project follows [Semantic Versioning](https://semver.org/) and this file follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Initial public release of the QueryFlow toolkit:
  - `QueryFlow.Abstractions` — request/response models, enums, exceptions, options.
  - `QueryFlow.Expressions` — cached, provider-translatable expression-tree builders for filtering, sorting, search, field selection, cursor seek predicates, and aggregates.
  - `QueryFlow.Validation` — `QueryableConfig<T>` allowlist builder and request validator.
  - `QueryFlow.Core` — `Filter`, `Search`, `Sort`, `SelectFields`, `Paginate`/`PaginateAsync`, `CursorPaginate`/`CursorPaginateAsync`, `AggregateAsync`.
  - `QueryFlow.EFCore` — `Include()` eager loading and `ApplyQuery()` composition.
  - `QueryFlow.Dapper` — `DapperQueryBuilder<T>` for safe, parameterized SQL fragment generation (SQL Server, PostgreSQL, MySQL, SQLite paging syntax).
  - `QueryFlow.AspNetCore` — query-string DSL parser, minimal API model binding, DI registration, `QueryFlowException` → `ProblemDetails` middleware.
  - `QueryFlow.OpenApi` — Swashbuckle operation filter for the standard query parameters.
- HMAC-signed, bidirectional keyset cursor pagination.
- Runtime-emitted projection types for genuine column-level `SELECT` on field selection with EF Core.
- Sample Web API (`samples/QueryFlow.Samples.WebApi`) demonstrating the full feature set against SQLite/InMemory EF Core.
- BenchmarkDotNet suite covering filtering, sorting, pagination, and field selection.
- 176 tests across unit and EF Core/Dapper integration (against real SQLite) test projects.

[Unreleased]: https://github.com/Asif-Nawaz27/QueryFlow/commits/main
