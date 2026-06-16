# StringThing.FSharp.Npgsql

Injection-safe interpolated SQL for PostgreSQL via [Npgsql](https://www.npgsql.org/), F# edition. Part of [StringThing.FSharp](https://github.com/immersus-machina/string-thing/tree/main/fsharp).

## Install

```
dotnet add package StringThing.FSharp.Npgsql
```

The package bundles the matching analyzer DLL — installing it activates the analyzer automatically. It recovers compile-time parameter-type checking and fragment-provenance enforcement that the `FormattableString`-based runtime dispatch otherwise defers to runtime.

## Quick start

```fsharp
open Npgsql
open StringThing.FSharp
open StringThing.FSharp.Npgsql

use connection = new NpgsqlConnection("Host=localhost;Database=app;Username=postgres;Password=...")
connection.Open()

let userId = 42L
let name : string =
    connection.QueryStringSingle Row.scalar $"""
        SELECT name FROM users WHERE id = {userId}
        """
```

Parameters are positional (`$1`, `$2`, ...) — PostgreSQL's native convention, set up automatically by the walker. The call site looks like a C#-style interpolated string, but the values stay typed at runtime: F# `$""` lowers to `FormattableString`, which StringThing walks to bind each `{value}` hole to a typed `NpgsqlParameter`.

Use F#'s triple-quoted form `$""" """` for multi-line SQL — the regex parser ignores whitespace, and the SQL engine ignores extra newlines.

## Result mapping

Build a row reader with the `row { let! ... and! ... return ... }` computation expression:

```fsharp
type User = { Id: int64; Name: string; Email: string option }

let userRow : RowReader<User> =
    row {
        let! id    = Row.int64 "id"
        and! name  = Row.string "name"
        and! email = Row.stringOption "email"
        return { Id = id; Name = name; Email = email }
    }

let user =
    connection.QueryStringSingle userRow $"""
        SELECT id, name, email
        FROM users
        WHERE id = {userId}
        """

let users =
    connection.QueryString userRow $"""
        SELECT id, name, email
        FROM users
        ORDER BY id
        """
    |> Seq.toList
```

Connection methods: `QueryStringSingle`, `QueryStringSingleOrDefault`, `QueryString` (returns lazy `seq<'T>`), `ExecuteString`, `ExecuteStringScalar`.

Ordinals are resolved once per `(format string, row type)` pair and cached, so `GetOrdinal` runs at most once per call site per row type.

## Scalar queries

When the query returns a single column, pass `Row.scalar` (or `Row.scalarOption` for nullable columns):

```fsharp
let count : int64 =
    connection.QueryStringSingle Row.scalar $"""
        SELECT COUNT(*) FROM users
        """

let email : string option =
    connection.QueryStringSingle Row.scalarOption $"""
        SELECT email FROM users WHERE id = {userId}
        """
```

## Supported parameter types

Scalars: `bool`, `int16`, `int`, `int64`, `float32`, `float`, `decimal`, `char`, `string`, `byte[]`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, plus `'T option` for any of the above.

1D arrays (map to PostgreSQL native array columns): `int[]`, `int64[]`, `string[]`, `Guid[]`, `bool[]`, `float[]`, `decimal[]`.

Embedded `PostgresFragment` values (see below) and `obj` returns from `Postgres.unsafe` / `Postgres.inList` / `Postgres.insertRows`.

PostgreSQL has a much richer type system than the runtime dispatch currently covers (jsonb, hstore, ranges, network types, geometric types, etc.). Pass them as raw SQL via `Postgres.unsafe` or write a typed wrapper — these can be added to the dispatch as v2 if usage demands.

Unsupported types throw `InvalidOperationException` at runtime. The bundled analyzer makes this a compile-time error.

## PostgresStatement and PostgresFragment

Two nominal wrapper types signal intent and let the analyzer prove provenance:

- **`PostgresStatement`** — a complete top-level query. Build with `Postgres.statement $"..."`.
- **`PostgresFragment`** — a composable piece spliced into a larger statement. Build with `Postgres.fragment $"..."`.

Both are aliases for the generic `SqlStatement<NpgsqlParameter>` / `SqlFragment<NpgsqlParameter>` types from `StringThing.FSharp`. They have `op_Implicit` conversions to `FormattableString`, so they flow naturally into connection methods that take `FormattableString`.

```fsharp
let userById id =
    Postgres.statement $"""
        SELECT id, name, email
        FROM users
        WHERE id = {id}
        """

let user = connection.QueryStringSingle userRow (userById 42L)
```

## Suppressing the FS3391 advisory

The `op_Implicit` conversion from `PostgresStatement` / `PostgresFragment` to `FormattableString` is F# 9's "additional implicit conversions" feature. The compiler emits an advisory warning each time it fires:

```
warning FS3391: This expression uses the implicit conversion
'SqlStatement<NpgsqlParameter> -> FormattableString'.
```

The warning exists because implicit conversions are unusual in F# and the compiler wants you to know one happened. In this library it's expected at every call site that passes a wrapper to a connection method, so the advisory is noise. Suppress it file-wide:

```fsharp
#nowarn "3391"
```

at the top of any source file that uses these wrappers. Only suppresses the implicit-conversion advisory; doesn't change runtime behaviour.

## Fragment composition

Embed a `PostgresFragment` value inside another `$""`:

```fsharp
let activeAdults : PostgresFragment =
    Postgres.fragment $"age >= 18 AND active = true"

let users =
    connection.QueryString userRow $"""
        SELECT id, name, email
        FROM users
        WHERE {activeAdults}
        """
```

Inline composition also works — embed a `$""` directly inside another `$""`:

```fsharp
let minId = 1L
let users =
    connection.QueryString userRow $"""
        SELECT id, name, email
        FROM users
        WHERE {$"id > {minId}"}
        """
```

The embedded fragment's parameters renumber and splice in place automatically.

## Multi-row insert

```fsharp
type InsertUser = { Id: int64; Name: string; Email: string option }

let insertUserRow (u: InsertUser) : PostgresFragment =
    Postgres.fragment $"({u.Id}, {u.Name}, {u.Email}, true)"

let users = [
    { Id = 1L; Name = "alice"; Email = Some "alice@example.com" }
    { Id = 2L; Name = "bob"; Email = None }
]

connection.ExecuteString $"""
    INSERT INTO users (id, name, email, active)
    VALUES {Postgres.insertRows insertUserRow users}
    """
    |> ignore
```

## IN list

```fsharp
let ids = [1L; 2L; 3L]

let users =
    connection.QueryString userRow $"""
        SELECT id, name, email
        FROM users
        WHERE id IN {Postgres.inList ids}
        """
    |> Seq.toList
```

## PostgreSQL array parameters

The runtime dispatch maps F# 1D arrays to PostgreSQL native array types, so you can pass them as parameters and use them with `ANY(...)`, `= ANY(...)`, array operators, etc.:

```fsharp
let ids : int64[] = [| 1L; 3L |]

let users =
    connection.QueryString userRow $"""
        SELECT id, name, email
        FROM users
        WHERE id = ANY({ids})
        """
    |> Seq.toList
```

Pair with PostgreSQL's array column syntax (`integer[]`, `text[]`, etc.) at the schema level.

## Unsafe escape hatch

```fsharp
let tableName = Postgres.unsafe "users"
let userId = 42L

let user =
    connection.QueryStringSingle userRow $"""
        SELECT id, name, email
        FROM {tableName}
        WHERE id = {userId}
        """
```

`Postgres.unsafe` splices raw, unparameterized SQL — the caller takes responsibility for safety.

---

Built by [Immersus Machina](https://www.immersus-machina.com)
