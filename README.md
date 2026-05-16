# StringThing

Interpolated SQL parameterization for .NET — injection-safe by construction, with per-provider type checking where the provider supports it.

Built on C# interpolated string handlers. The compiler binds every `{parameter}` to a typed parameter at build time, with no string concatenation.

## The idea

```csharp
var userId = 42;
var threshold = TimeSpan.FromHours(36);

// Postgres — TimeSpan maps to interval, any duration works
PostgresSql statement = $"SELECT * FROM jobs WHERE user_id = {userId} AND elapsed > {threshold}";

// SQL Server — named parameters automatically
SqlServerSql statement = $"SELECT * FROM jobs WHERE user_id = {userId} AND elapsed > {threshold}";
// produces: ... WHERE user_id = @userId AND elapsed > @threshold
```

One package per provider. No string overload — the only path is `$"..."`.

## What this gives you

- **Injection safety by construction.** There is no way to pass a raw string as SQL. `Sql.Unsafe()` is the explicit, auditable escape hatch.
- **Parameter deduplication.** `$"WHERE a = {x} OR b = {x}"` produces one parameter. By variable identity, not value.
- **Composable fragments.** Build `WHERE` clauses as typed fragments. Splice them in. Parameters renumber automatically.
- **Multi-row inserts.** `InsertRows` for VALUES composition, TVPs for SQL Server.
- **AOT-compatible result mapping.** Mark POCOs with `[StringThingRow] partial` and a source generator emits the row materializer at compile time. No reflection, no IL emit — runs under `PublishAot`.

## Provider depth

Not all providers deliver the same depth of type checking. The depth depends on how much the underlying driver distinguishes between parameter types.

| Provider | Type safety | What it checks |
|----------|-------------|----------------|
| **Postgres** | Deep | 87 typed variants — geometric types, ranges, tsvector, arrays. Each maps to a specific `NpgsqlDbType`. Unsupported types are a compile error. |
| **SQL Server** | Moderate | 17 types mapped to specific `SqlDbType`. Named parameters via `CallerArgumentExpression`. TVP support. |
| **MySQL** | Moderate | 22 types mapped to specific `MySqlDbType`. Unsigned integer support. |
| **SQLite** | Light | SQLite is dynamically typed — all values are stored as TEXT, INTEGER, REAL, BLOB, or NULL. The type contract is thin. The value is injection safety and parameterization ergonomics, not type checking. |

All providers share: injection safety, parameter deduplication, composable fragments, multi-row inserts, and AOT-compatible result mapping.

## Packages

| Package | Built on |
|---------|----------|
| `StringThing.Npgsql` | Npgsql |
| `StringThing.SqlClient` | Microsoft.Data.SqlClient |
| `StringThing.MySql` | MySqlConnector |
| `StringThing.Sqlite` | Microsoft.Data.Sqlite |

Each provider includes AOT-compatible result mapping. Annotate row POCOs with `[StringThingRow] partial` and call `connection.QueryString<T>($"...")` — a source generator emits the materializer. See each provider's README for details.

## Performance

Benchmarked against raw ADO.NET and Dapper on SQLite in-memory. Queries return one row (`LIMIT 1`) to keep materialization cost a small, constant baseline across implementations. Raw ADO.NET subtracted to show pure library overhead.

### Query and insert overhead

| Scenario | Dapper | StringThing | ST vs Dapper |
|----------|--------|-------------|--------------|
| QuerySingle 1 param | +1.03 us / +0.68 KB | +0.35 us / +0.18 KB | -66% time, -73% alloc |
| Query 2 params | +1.20 us / +0.89 KB | +0.25 us / +0.19 KB | -79% time, -79% alloc |
| Query 5 params | +1.64 us / +1.54 KB | +0.32 us / +0.21 KB | -80% time, -86% alloc |
| Execute insert | +0.81 us / +1.12 KB | +0.14 us / +0.22 KB | -83% time, -80% alloc |

### IN list overhead

| Scenario | Dapper | StringThing | ST vs Dapper |
|----------|--------|-------------|--------------|
| IN 10 items | +3.84 us / +3.87 KB | +2.04 us / +3.55 KB | -47% time, -8% alloc |
| IN 100 items | +72.83 us / +28.23 KB | +17.02 us / +24.32 KB | -77% time, -14% alloc |

StringThing is faster than Dapper and allocates less on every measured scenario after subtracting the raw ADO.NET cost. The gap widens on parameter-heavy paths — a 5-param query has 80% less time overhead and 86% less allocation, and an `Execute` insert has 83% less time and 80% less allocation. IN list expansion pulls the furthest ahead on time: Dapper rewrites the SQL string at runtime, StringThing builds the parameterized list directly, finishing at 77% less time on a 100-item list.

The source-generated row mapper resolves column ordinals once per call site and caches them, so subsequent queries skip all name→ordinal lookups — the dominant per-row allocation cost on most drivers.

Benchmark source: [EndToEndBenchmarks.cs](benchmarks/StringThing.Benchmarks/EndToEndBenchmarks.cs), [InListBenchmarks.cs](benchmarks/StringThing.Benchmarks/InListBenchmarks.cs), [CommandCreationBenchmarks.cs](benchmarks/StringThing.Benchmarks/CommandCreationBenchmarks.cs), [InsertRowsBenchmarks.cs](benchmarks/StringThing.Benchmarks/InsertRowsBenchmarks.cs)

## Analyzer

StringThing ships a Roslyn analyzer that runs in your IDE and at build time. One rule:

| ID | Severity | Description |
|----|----------|-------------|
| ST0001 | Error | Multiple interpolated SQL statements on the same source line |

StringThing caches interpolated string templates by source location. Two SQL statements on the same line share a cache key, which causes silent incorrect parameter binding. The analyzer prevents this at compile time.

```csharp
// ST0001 — move to separate lines
PostgresSql statement = condition ? $"SELECT {x}" : $"SELECT {y}";

// OK
PostgresSql statement = condition
    ? $"SELECT {x}"
    : $"SELECT {y}";
```

The analyzer is included automatically when you reference any StringThing package.

## Caching

StringThing caches the SQL template for each call site after the first execution, keyed on `CallerFilePath` + `CallerLineNumber`. Subsequent calls at the same site skip all string building and parameter name resolution. Cache entries are never evicted — the total number is bounded by the number of unique SQL statements in your compiled code, not by runtime behavior. Each entry is roughly the size of the SQL string plus a small array for parameter metadata.

When using `QueryString<T>`, resolved column ordinals are also cached per `(call site, T)`, so name → ordinal lookups run only once per source location and row type.

## What it is not

Not a driver. Not an ORM. Not a query builder. It sits on top of ADO.NET providers and adds parameterized interpolation they don't have.

## Prior art

- **[`porsager/postgres`](https://github.com/porsager/postgres)** (Node/TS) — tagged template literals as the only query API. The direct inspiration for "interpolation-only, no string escape hatch."
- **[`InterpolatedSql`](https://github.com/Drizin/InterpolatedSql)** — existing .NET library for interpolated SQL parameterization. Uses `FormattableString`, accepts any type at runtime, database-agnostic. StringThing differs by restricting types per provider at compile time.
- **EF Core `FromSqlInterpolated`** — proves the ergonomic model works for .NET developers. StringThing removes the EF dependency.

---

Built by [Immersus Machina](https://www.immersus-machina.com)
