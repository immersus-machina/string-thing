# StringThing

Working thesis: interpolated SQL in C# should be **compile-time type-safe against the target database provider**.

Built on C# interpolated string handlers — the compiler resolves every `{parameter}` at build time, not runtime.

## The idea

```csharp
var userId = 42;
var threshold = TimeSpan.FromHours(36);

// Postgres — TimeSpan maps to interval, any duration works
PostgresSql stmt = $"SELECT * FROM jobs WHERE user_id = {userId} AND elapsed > {threshold}";

// SQL Server — named parameters automatically
SqlServerSql stmt = $"SELECT * FROM jobs WHERE user_id = {userId} AND elapsed > {threshold}";
// produces: ... WHERE user_id = @userId AND elapsed = @threshold
```

One package per provider. The handler accepts only types the target provider supports. The compiler enforces the contract — not the runtime, not the driver, not the database.

## What this gives you

- **Injection safety by construction.** No string overload. The only path is `$"..."`.
- **Parameter deduplication.** `$"WHERE a = {x} OR b = {x}"` produces one parameter. By variable identity, not value.
- **Composable fragments.** Build `WHERE` clauses as typed fragments. Splice them in. Parameters renumber automatically.
- **Batch inserts.** Multi-row VALUES via `InsertRows`, UNNEST for Postgres, TVPs for SQL Server.
- **Explicit unsafe escape hatch.** `Sql.Unsafe("table_name")` is the only way to inject raw text.

## Packages

| Package | Purpose |
|---------|---------|
| `StringThing.Npgsql` | Postgres provider |
| `StringThing.SqlClient` | SQL Server provider |
| `StringThing.Npgsql.Dapper` | Dapper integration for Postgres |
| `StringThing.SqlClient.Dapper` | Dapper integration for SQL Server |

## What it is not

Not a driver. Not an ORM. It sits on top of Npgsql or Microsoft.Data.SqlClient and adds the compile-time type contract they don't have.

## Prior art

- **[`porsager/postgres`](https://github.com/porsager/postgres)** (Node/TS) — tagged template literals as the only query API. The direct inspiration for "interpolation-only, no string escape hatch."
- **[`InterpolatedSql`](https://github.com/Drizin/InterpolatedSql)** — existing .NET library for interpolated SQL parameterization. Uses `FormattableString`, accepts any type at runtime, database-agnostic.
- **EF Core `FromSqlInterpolated`** — proves the ergonomic model works for .NET developers. StringThing removes the EF dependency.
