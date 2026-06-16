# StringThing.FSharp

Injection-safe interpolated SQL for F#. Call sites read like a SQL statement, values stay typed, and an optional per-provider analyzer catches the rest at compile time.

```fsharp
open StringThing.FSharp
open StringThing.FSharp.Sqlite     // or .MySql / .SqlClient / .Npgsql

let userId = 42L
let user =
    connection.QueryStringSingle userRow $"""
        SELECT id, name, email
        FROM users
        WHERE id = {userId}
        """
```

F# `$""" """` triple-quoted interpolation lowers to `FormattableString`; the provider walks it at runtime and dispatches each `{value}` hole to a typed parameter for its driver (`SqliteParameter`, `MySqlParameter`, `SqlParameter`, `NpgsqlParameter`). Unsupported types throw at runtime with a helpful message — and the optional per-provider analyzer elevates those failures to compile-time errors in your editor, plus enforces that no opaque `FormattableString` slips through.

## Row readers

Define a `RowReader<'T>` with the `row { ... }` computation expression using F#'s applicative `let! / and!` syntax:

```fsharp
type User = { Id: int64; Name: string; Email: string option }

let userRow : RowReader<User> =
    row {
        let! id    = Row.int64 "id"
        and! name  = Row.string "name"
        and! email = Row.stringOption "email"
        return { Id = id; Name = name; Email = email }
    }
```

For single-column queries, skip the row reader entirely:

```fsharp
let count : int64 =
    connection.QueryStringSingle Row.scalar $"""
        SELECT COUNT(*) FROM users
        """
```

## Composing queries

Each provider exposes two nominal types — `<Provider>Statement` and `<Provider>Fragment` — that let you factor query construction without losing analyzer enforcement:

```fsharp
// SqliteStatement — a top-level query
let userById id =
    Sqlite.statement $"SELECT id, name, email FROM users WHERE id = {id}"

let user = connection.QueryStringSingle userRow (userById 42L)

// SqliteFragment — a piece spliced into a larger statement
let activeAdults : SqliteFragment =
    Sqlite.fragment $"age >= 18 AND active = 1"

let users =
    connection.QueryString userRow $"""
        SELECT id, name, email FROM users WHERE {activeAdults}
        """
```

Both wrappers are aliases for the generic `SqlStatement<'TParameter>` / `SqlFragment<'TParameter>` types in `StringThing.FSharp` — every provider uses the same nominal shape, only the parameter type differs. They have `op_Implicit` conversions to `FormattableString` so they flow naturally into the connection methods.

For MySQL substitute `MySql` for `Sqlite`; for SQL Server use `SqlServer` (with `SqlServerStatement` / `SqlServerFragment`); for PostgreSQL use `Postgres` (with `PostgresStatement` / `PostgresFragment`). The connection-method names and call-site shapes are identical across providers.

## Suppressing the FS3391 advisory

The `op_Implicit` conversions from the wrapper types to `FormattableString` are F# 9's "additional implicit conversions" feature, which the compiler flags with an advisory warning each time it fires:

```
warning FS3391: This expression uses the implicit conversion
'SqlStatement<'TParameter> -> FormattableString'.
```

The warning exists because implicit conversions are unusual in F# and the compiler wants you to know one happened. In this library it's expected behaviour at every call site that uses a factored wrapper, so the advisory is noise. Suppress it file-wide:

```fsharp
#nowarn "3391"
```

at the top of any source file that uses these wrappers. Only suppresses the implicit-conversion advisory; doesn't change runtime behaviour.

## What the runtime walker does

Beyond scalar parameters:

- **Composable fragments** — embed a `<Provider>Fragment` or another `$""` inside a hole and its parameters renumber and splice automatically.
- **`<Provider>.inList values`** — expands to `(@p0, @p1, @p2)` (or `($1, $2, $3)` for Postgres) with parameters added in order.
- **`<Provider>.insertRows toRow rows`** — composes multi-row VALUES clauses from a row-shaping function.
- **`<Provider>.unsafe raw`** — splices literal SQL with no parameterization (your responsibility).
- **`Option<T>` columns** — `Some v` binds the value, `None` binds SQL `NULL`. No manual `DBNull.Value` plumbing.

And the analyzer adds a compile-time guarantee on top: only verified-provenance `FormattableString` values reach connection methods. Helpers that return `<Provider>Statement` / `<Provider>Fragment` are accepted; helpers that return raw `FormattableString` are rejected at the call site.

## Packages

| Package | Purpose |
|---|---|
| [`StringThing.FSharp`](src/StringThing.FSharp/) | Core — `RowReader<'T>`, `row { }` CE, `SqlStatement<'TParameter>` / `SqlFragment<'TParameter>` wrapper types, `SqlElement` tree, ordinal cache |
| [`StringThing.FSharp.Sqlite`](src/StringThing.FSharp.Sqlite/) | SQLite via Microsoft.Data.Sqlite — `SqliteStatement` / `SqliteFragment`, `Sqlite.statement` / `.fragment` / `.unsafe` / `.inList` / `.insertRows`, connection extensions |
| [`StringThing.FSharp.MySql`](src/StringThing.FSharp.MySql/) | MySQL via MySqlConnector — `MySqlStatement` / `MySqlFragment`, `MySql.statement` / etc. |
| [`StringThing.FSharp.SqlClient`](src/StringThing.FSharp.SqlClient/) | SQL Server via Microsoft.Data.SqlClient — `SqlServerStatement` / `SqlServerFragment`, `SqlServer.statement` / etc. |
| [`StringThing.FSharp.Npgsql`](src/StringThing.FSharp.Npgsql/) | PostgreSQL via Npgsql — `PostgresStatement` / `PostgresFragment`, `Postgres.statement` / etc. Uses `$1, $2, ...` positional parameters. |

Each provider package bundles its analyzer (and the shared analyzer core) directly under `analyzers/dotnet/fs/` — installing the provider activates the analyzer automatically. The analyzer source projects live under [`src/`](src/) but aren't published as standalone NuGet packages.

## Performance

About 1 µs of overhead per query above raw ADO.NET. Boxing for `FormattableString.GetArguments()` is the cost of using F#'s native interpolation. For workloads up to ~10K queries/sec it's well within Gen0 GC handling.

Benchmark sources: [EndToEndBenchmarks.fs](benchmarks/StringThing.FSharp.Benchmarks/EndToEndBenchmarks.fs), [InListBenchmarks.fs](benchmarks/StringThing.FSharp.Benchmarks/InListBenchmarks.fs).

## Differences from the [C# edition](https://github.com/immersus-machina/string-thing)

C# StringThing uses `[InterpolatedStringHandler]` — a compile-time mechanism F# doesn't honour. The F# edition uses `FormattableString` instead. The visible consequences:

- Parameter values are boxed into `object[]` once per query.
- Per-parameter type checking is at **runtime** by default; the per-provider analyzer raises it to compile time.
- Parameter names are positional (`@p0` / `$1` etc.) — F# `$""` doesn't surface the source expression like C#'s `CallerArgumentExpression` does.

Everything else — injection safety, composable fragments, IN list expansion, multi-row inserts, raw-SQL escape hatch, ordinal caching, `Option<T>` for nullables, the `SqlStatement<'TParameter>` / `SqlFragment<'TParameter>` nominal types — is the same.

---

Built by [Immersus Machina](https://www.immersus-machina.com)
