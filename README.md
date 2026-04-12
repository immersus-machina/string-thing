# StringThing

Interpolated SQL parameterization for .NET — injection-safe by construction, with per-provider type checking where the provider supports it.

Built on C# interpolated string handlers. The compiler resolves every `{parameter}` at build time, not runtime.

## The idea

```csharp
var userId = 42;
var threshold = TimeSpan.FromHours(36);

// Postgres — TimeSpan maps to interval, any duration works
PostgresSql stmt = $"SELECT * FROM jobs WHERE user_id = {userId} AND elapsed > {threshold}";

// SQL Server — named parameters automatically
SqlServerSql stmt = $"SELECT * FROM jobs WHERE user_id = {userId} AND elapsed > {threshold}";
// produces: ... WHERE user_id = @userId AND elapsed > @threshold
```

One package per provider. No string overload — the only path is `$"..."`.

## What this gives you

- **Injection safety by construction.** There is no way to pass a raw string as SQL. `Sql.Unsafe()` is the explicit, auditable escape hatch.
- **Parameter deduplication.** `$"WHERE a = {x} OR b = {x}"` produces one parameter. By variable identity, not value.
- **Composable fragments.** Build `WHERE` clauses as typed fragments. Splice them in. Parameters renumber automatically.
- **Multi-row inserts.** `InsertRows` for VALUES composition, TVPs for SQL Server.

## Provider depth

Not all providers deliver the same depth of type checking. The depth depends on how much the underlying driver distinguishes between parameter types.

| Provider | Type safety | What it checks |
|----------|-------------|----------------|
| **Postgres** | Deep | 87 typed variants — geometric types, ranges, tsvector, arrays. Each maps to a specific `NpgsqlDbType`. Unsupported types are a compile error. |
| **SQL Server** | Moderate | 17 types mapped to specific `SqlDbType`. Named parameters via `CallerArgumentExpression`. TVP support. |
| **MySQL** | Moderate | 22 types mapped to specific `MySqlDbType`. Unsigned integer support. |
| **SQLite** | Light | SQLite is dynamically typed — all values are stored as TEXT, INTEGER, REAL, BLOB, or NULL. The type contract is thin. The value is injection safety and parameterization ergonomics, not type checking. |

All providers share: injection safety, parameter deduplication, composable fragments, and multi-row inserts.

## Packages

| Package | Built on |
|---------|----------|
| `StringThing.Npgsql` | Npgsql |
| `StringThing.SqlClient` | Microsoft.Data.SqlClient |
| `StringThing.MySql` | MySqlConnector |
| `StringThing.Sqlite` | Microsoft.Data.Sqlite |

Each provider has a `.Dapper` companion package that adds Dapper result mapping with internalized Dapper (not exposed as a dependency).

## Performance

Benchmarked against raw ADO.NET and Dapper on SQLite in-memory. Queries return one row (`LIMIT 1`) to isolate parameter binding overhead from result set materialization. Raw ADO.NET subtracted to show pure library overhead.

### Query and insert overhead

| Scenario | Dapper | StringThing | ST vs Dapper |
|----------|--------|-------------|--------------|
| QuerySingle 1 param | +1.03 us / +0.68 KB | +0.99 us / +0.60 KB | -4% time, -12% alloc |
| Query 2 params | +1.20 us / +0.89 KB | +1.09 us / +0.60 KB | -9% time, -33% alloc |
| Query 5 params | +1.64 us / +1.54 KB | +1.04 us / +0.62 KB | -37% time, -60% alloc |
| Execute insert | +0.81 us / +1.12 KB | +0.23 us / +0.20 KB | -72% time, -82% alloc |

### IN list overhead

| Scenario | Dapper | StringThing | ST vs Dapper |
|----------|--------|-------------|--------------|
| IN 10 items | +3.84 us / +3.87 KB | +2.55 us / +3.99 KB | -34% time |
| IN 100 items | +72.83 us / +28.23 KB | +14.79 us / +26.86 KB | -80% time |

StringThing is faster than Dapper on every measured scenario after subtracting the raw ADO.NET cost, with consistently lower allocations (no anonymous object reflection). The gap widens on parameter-heavy paths — a 5-param query has 60% less allocation overhead, and an `Execute` insert has 72% less time overhead. IN list expansion pulls the furthest ahead — Dapper rewrites the SQL string at runtime, StringThing builds the parameterized list directly.

## Analyzer

StringThing ships a Roslyn analyzer that runs in your IDE and at build time. One rule:

| ID | Severity | Description |
|----|----------|-------------|
| ST0001 | Error | Multiple interpolated SQL statements on the same source line |

StringThing caches interpolated string templates by source location. Two SQL statements on the same line share a cache key, which causes silent incorrect parameter binding. The analyzer prevents this at compile time.

```csharp
// ST0001 — move to separate lines
var stmt = condition ? (PostgresSql)$"SELECT {x}" : (PostgresSql)$"SELECT {y}";

// OK
var stmt = condition
    ? (PostgresSql)$"SELECT {x}"
    : (PostgresSql)$"SELECT {y}";
```

The analyzer is included automatically when you reference any StringThing package.

## Caching

StringThing caches the SQL template for each call site after the first execution, keyed on `CallerFilePath` + `CallerLineNumber`. Subsequent calls at the same site skip all string building and parameter name resolution. Cache entries are never evicted — the total number is bounded by the number of unique SQL statements in your compiled code, not by runtime behavior. Each entry is roughly the size of the SQL string plus a small array for parameter metadata.

## What it is not

Not a driver. Not an ORM. Not a query builder. It sits on top of ADO.NET providers and adds parameterized interpolation they don't have.

## Prior art

- **[`porsager/postgres`](https://github.com/porsager/postgres)** (Node/TS) — tagged template literals as the only query API. The direct inspiration for "interpolation-only, no string escape hatch."
- **[`InterpolatedSql`](https://github.com/Drizin/InterpolatedSql)** — existing .NET library for interpolated SQL parameterization. Uses `FormattableString`, accepts any type at runtime, database-agnostic. StringThing differs by restricting types per provider at compile time.
- **EF Core `FromSqlInterpolated`** — proves the ergonomic model works for .NET developers. StringThing removes the EF dependency.
