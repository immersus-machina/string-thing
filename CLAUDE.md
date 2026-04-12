# StringThing

Compile-time type-safe interpolated SQL for .NET. Built on C# 10 interpolated string handlers.

## Project structure

- `src/StringThing.Core/` — `UnsafeSql`, `Sql.Unsafe()`, `IParameterNamer` (namespace: `StringThing`)
- `src/StringThing.Npgsql/` — `SqlStatement<TNamer>`, `SqlFragment`, Postgres-specific overloads (namespace: `StringThing.Npgsql`)
- `src/StringThing.Analyzers/` — Roslyn analyzer (targets `netstandard2.0`), ships `ST0001`
- `test/StringThing.Npgsql.Tests/` — xUnit v3 tests, runs as exe via `dotnet run`

## Analyzer: ST0001

`SqlStatement` caches interpolated string templates keyed on `(CallerFilePath, CallerLineNumber)`. Two handler invocations on the same source line share a cache key, causing silent incorrect parameter binding. The analyzer `ST0001` makes this a compile error.

The analyzer is wired to all projects in the solution via `Directory.Build.props`. For NuGet distribution, include the analyzer DLL under `analyzers/dotnet/cs/` in the package.

## Build and test

```
dotnet build
dotnet run --project test/StringThing.Npgsql.Tests
```

Target framework: `net10.0`.

## Code style

The code must be self-documenting. Favor clarity from identifiers, types, and structure rather than from comments.

- **Verbose, descriptive identifiers. No acronyms.** `parameterBuffer` over `paramBuf`, `interpolatedStringHandler` over `isHandler`. Industry-universal abbreviations are allowed (`Sql`, `Http`, `Url`).
- **Comments are forbidden except for unobvious business logic.** Do not write comments that restate what the code plainly does. Write a comment only when there is a reason behind the code that cannot be expressed _in_ the code — a platform quirk, a performance trick with surprising semantics, a workaround for a specific bug, a business rule that cannot be named.
- **XML documentation comments on public API members are allowed** but must be short and functional (one or two sentences). They are API documentation, not explanations.
- When editing existing code that has long explanatory comments, strip them.

## Test conventions

Tests live under `test/`, one project per library being tested, named `{ProjectUnderTest}.Tests.csproj`. The test framework is **xUnit v3** (`xunit.v3` package only — neither `Microsoft.NET.Test.Sdk` nor `xunit.runner.visualstudio` is required, because xUnit v3 ships its own in-process runner that runs via `dotnet run`).

Test code follows different conventions than production code. The "no comments" rule from the production code style does **not** apply to tests; tests get an explicit exception for structural Arrange/Act/Assert markers.

- **`// Arrange`, `// Act`, and `// Assert` comments mark the sections that exist.** Each section that has actual code gets its corresponding comment. Omit the comment for any section that has no code — if a test has no arrange step, it begins with `// Act`. The comments are structural markers for sections that exist, not placeholders for sections that don't.
- **Test names follow `SubjectMethod_WhenCondition_ExpectedOutcome`**, in PascalCase with underscores between the three sections. The test class already names the type, so SubjectMethod names the specific method under test. For general behavior tests where no single method is the subject, SubjectMethod can be omitted (e.g. `WhenInterpolatingSingleInteger_CapturesAsDollarOnePlaceholder`). For specific method tests, include it (e.g. `Dispose_WhenCalledTwice_DoesNotThrow`).
- **Expected values are declared in the Assert section**, not in Arrange. Inputs belong to Arrange; expected outputs belong with the assertion that uses them. When the expected value depends on Arrange variables, reference them rather than duplicating literals.
