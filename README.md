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
| QuerySingle 1 param | +0.94 us / +0.68 KB | +1.03 us / +0.63 KB | +10% time, -7% alloc |
| Query 2 params | +1.28 us / +0.89 KB | +1.17 us / +0.64 KB | -9% time, -28% alloc |
| Query 5 params | +1.68 us / +1.54 KB | +1.54 us / +0.81 KB | -8% time, -47% alloc |
| Execute insert | +0.86 us / +1.12 KB | +0.39 us / +0.36 KB | -54% time, -68% alloc |

### IN list overhead

| Scenario | Dapper | StringThing | ST vs Dapper |
|----------|--------|-------------|--------------|
| IN 10 items | +4.28 us / +3.87 KB | +2.60 us / +3.99 KB | -39% time |
| IN 100 items | +71.58 us / +28.23 KB | +13.61 us / +26.86 KB | -81% time |

Standard queries are within ~10% of Dapper, with consistently lower allocations (no anonymous object reflection). IN list expansion is where StringThing pulls ahead — Dapper rewrites the SQL string at runtime, StringThing builds the parameterized list directly.

## What it is not

Not a driver. Not an ORM. Not a query builder. It sits on top of ADO.NET providers and adds parameterized interpolation they don't have.

## Prior art

- **[`porsager/postgres`](https://github.com/porsager/postgres)** (Node/TS) — tagged template literals as the only query API. The direct inspiration for "interpolation-only, no string escape hatch."
- **[`InterpolatedSql`](https://github.com/Drizin/InterpolatedSql)** — existing .NET library for interpolated SQL parameterization. Uses `FormattableString`, accepts any type at runtime, database-agnostic. StringThing differs by restricting types per provider at compile time.
- **EF Core `FromSqlInterpolated`** — proves the ergonomic model works for .NET developers. StringThing removes the EF dependency.
