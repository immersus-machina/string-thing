# string-thing

Working thesis: interpolated SQL in C# should be **compile-time type-safe against the target database provider**.

Built on C# 10 interpolated string handlers — the compiler resolves every `{parameter}` at build time, not runtime.

## The idea

```csharp
async Task<List<Job>> GetLongRunningJobs(DbConnection db, TimeSpan threshold)
{
    // Npgsql handler — TimeSpan maps to interval, any duration works
    return await db.QueryAsync<Job>(
        $"SELECT * FROM jobs WHERE elapsed > {threshold}");  // compiles ✓

    // SqlClient handler — TimeSpan can't safely map to any SQL Server type
    return await db.QueryAsync<Job>(
        $"SELECT * FROM jobs WHERE elapsed > {threshold}");  // compile ERROR ✗
}
```

One package per provider. The handler's `AppendFormatted` overloads exist only for types the target provider supports. The compiler enforces the contract — not the runtime, not the driver, not the database.

## What this gives you

- **Injection safety by construction.** No string overload on `QueryAsync`. The only path is `$"..."`.
- **Parameter deduplication.** `$"WHERE a = {x} OR b = {x}"` produces one parameter slot. By variable identity, not value — two different variables holding `42` stay as two parameters.
- **Composable fragments.** Build `WHERE` clauses, filters, CTEs as typed `SqlFragment` values. Splice them in. Parameters merge and renumber automatically.
- **Explicit unsafe escape hatch.** `Sql.Unsafe("table_name")` is the only way to inject raw text. Named to be visible in code review.

## What it is not

Not a driver. Not an ORM. It sits on top of Npgsql or SqlClient and adds the compile-time type contract they don't have.

## Prior art

- **[`porsager/postgres`](https://github.com/porsager/postgres)** (Node/TS) — tagged template literals as the only query API. The direct inspiration for "interpolation-only, no string escape hatch."
- **[`InterpolatedSql`](https://github.com/Drizin/InterpolatedSql)** — existing .NET library for interpolated SQL parameterization. Uses `FormattableString`, accepts any type at runtime, database-agnostic.
- **EF Core `FromSqlInterpolated`** — proves the ergonomic model works for .NET developers. StringThing removes the EF dependency.
