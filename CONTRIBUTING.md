# Contributing to QueryFlow

Thanks for considering a contribution — QueryFlow is a community project and PRs, issues, and design discussion are all welcome.

## Getting started

```bash
git clone https://github.com/Asif-Nawaz27/QueryFlow.git
cd QueryFlow
dotnet restore QueryFlow.slnx
dotnet build QueryFlow.slnx
dotnet test QueryFlow.slnx
```

Requires the .NET 10 SDK (the solution multi-targets net8.0/net9.0/net10.0-compatible code but is built with the .NET 10 toolchain).

Run the sample API to see everything working end-to-end:

```bash
dotnet run --project samples/QueryFlow.Samples.WebApi
```

## Project layout

- `src/` — the packages themselves (see [Architecture Overview](docs/architecture.md) for how they relate).
- `tests/` — one xUnit + FluentAssertions project per `src/` package. EF Core tests run against a real SQLite database (not the lenient InMemory provider) specifically to catch SQL-translation bugs.
- `benchmarks/` — BenchmarkDotNet suite (`dotnet run -c Release --project benchmarks/QueryFlow.Benchmarks`).
- `samples/` — a runnable sample API demonstrating the full feature set.

## Before opening a PR

- **Add tests.** A bug fix should include a test that fails before the fix and passes after. New features need coverage in the relevant `tests/*` project.
- **Run the full suite:** `dotnet test QueryFlow.slnx`. All packages should build and test cleanly across net8.0/9.0/10.0 semantics (CI verifies net10.0; if you're unsure whether a change is TFM-sensitive, ask in the PR).
- **Keep changes focused.** Prefer several small PRs over one large one — easier to review, easier to bisect if something regresses.
- **Follow existing conventions.** No code comments unless they explain a non-obvious *why*; XML doc comments on public APIs; file-scoped namespaces; `.editorconfig` governs formatting (most IDEs pick it up automatically).

## Reporting bugs

Open an issue with: what you expected, what happened instead, and a minimal repro (a failing test is ideal, but a code snippet is fine). Include the QueryFlow package version and which provider (EF Core provider name, or Dapper + database) you're using.

## Proposing features

For anything beyond a small fix, open an issue first to discuss the design before investing in an implementation — this avoids wasted work on approaches that don't fit the project's direction (see [Architecture Overview](docs/architecture.md) for the current design principles).

## Good first issues

Issues labeled [`good first issue`](https://github.com/Asif-Nawaz27/QueryFlow/labels/good%20first%20issue) are scoped to be approachable without deep familiarity with the codebase. Comment on the issue before starting so two people don't end up working on the same thing.

## Code of Conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). By participating, you're expected to uphold it.

## License

By contributing, you agree your contributions will be licensed under the project's [MIT License](LICENSE).
