# StringThing.FSharp.SqlClient

Injection-safe interpolated SQL for SQL Server via [Microsoft.Data.SqlClient](https://learn.microsoft.com/sql/connect/ado-net/microsoft-ado-net-sql-server), F# edition. Part of [StringThing.FSharp](https://github.com/immersus-machina/string-thing/tree/main/fsharp).

## Install

```
dotnet add package StringThing.FSharp.SqlClient
```

The package bundles the matching analyzer DLL — installing it activates the analyzer automatically. It recovers compile-time parameter-type checking and fragment-provenance enforcement that the `FormattableString`-based runtime dispatch otherwise defers to runtime.

## Quick start

```fsharp
open Microsoft.Data.SqlClient
open StringThing.FSharp
open StringThing.FSharp.SqlClient

use connection = new SqlConnection("Server=localhost;Database=app;Integrated Security=true;TrustServerCertificate=true")
connection.Open()

let userId = 42L
let name : string =
    connection.QueryStringSingle Row.scalar $"""
        SELECT name FROM users WHERE id = {userId}
        """
```

Parameters are positional (`@p0`, `@p1`, ...). The call site looks like a C#-style interpolated string, but the values stay typed at runtime — F# `$""` lowers to `FormattableString`, which StringThing walks to bind each `{value}` hole to a typed `SqlParameter`.

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

When the query returns a single column, pass `Row.scalar` (or `Row.scalarOption` for nullable columns). Use `COUNT_BIG(*)` rather than `COUNT(*)` for `int64` results — SQL Server's `COUNT(*)` returns `int`.

```fsharp
let count : int64 =
    connection.QueryStringSingle Row.scalar $"""
        SELECT COUNT_BIG(*) FROM users
        """

let email : string option =
    connection.QueryStringSingle Row.scalarOption $"""
        SELECT email FROM users WHERE id = {userId}
        """
```

## Supported parameter types

`bool`, `int`, `int64`, `float`, `decimal`, `string`, `byte[]`, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, and `'T option` for any of the above. Embedded `SqlServerFragment` values (see below) and `obj` returns from `SqlServer.unsafe` / `SqlServer.inList` / `SqlServer.insertRows`.

For `DateOnly`, `TimeOnly`, table-valued parameters, and other less-common SQL Server types, cast at the call site or write a typed wrapper — these can be added to the dispatch as v2 if usage demands.

Unsupported types throw `InvalidOperationException` at runtime. The bundled analyzer makes this a compile-time error.

## SqlServerStatement and SqlServerFragment

Two nominal wrapper types signal intent and let the analyzer prove provenance:

- **`SqlServerStatement`** — a complete top-level query. Build with `SqlServer.statement $"..."`.
- **`SqlServerFragment`** — a composable piece spliced into a larger statement. Build with `SqlServer.fragment $"..."`.

Both are aliases for the generic `SqlStatement<SqlParameter>` / `SqlFragment<SqlParameter>` types from `StringThing.FSharp`. They have `op_Implicit` conversions to `FormattableString`, so they flow naturally into connection methods that take `FormattableString`.

```fsharp
let userById id =
    SqlServer.statement $"""
        SELECT id, name, email
        FROM users
        WHERE id = {id}
        """

let user = connection.QueryStringSingle userRow (userById 42L)
```

## Suppressing the FS3391 advisory

The `op_Implicit` conversion from `SqlServerStatement` / `SqlServerFragment` to `FormattableString` is F# 9's "additional implicit conversions" feature. The compiler emits an advisory warning each time it fires:

```
warning FS3391: This expression uses the implicit conversion
'SqlStatement<SqlParameter> -> FormattableString'.
```

The warning exists because implicit conversions are unusual in F# and the compiler wants you to know one happened. In this library it's expected at every call site that passes a wrapper to a connection method, so the advisory is noise. Suppress it file-wide:

```fsharp
#nowarn "3391"
```

at the top of any source file that uses these wrappers. Only suppresses the implicit-conversion advisory; doesn't change runtime behaviour.

## Fragment composition

Embed a `SqlServerFragment` value inside another `$""`:

```fsharp
let activeAdults : SqlServerFragment =
    SqlServer.fragment $"age >= 18 AND active = 1"

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

let insertUserRow (u: InsertUser) : SqlServerFragment =
    SqlServer.fragment $"({u.Id}, {u.Name}, {u.Email}, 1)"

let users = [
    { Id = 1L; Name = "alice"; Email = Some "alice@example.com" }
    { Id = 2L; Name = "bob"; Email = None }
]

connection.ExecuteString $"""
    INSERT INTO users (id, name, email, active)
    VALUES {SqlServer.insertRows insertUserRow users}
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
        WHERE id IN {SqlServer.inList ids}
        """
    |> Seq.toList
```

## Unsafe escape hatch

```fsharp
let tableName = SqlServer.unsafe "users"
let userId = 42L

let user =
    connection.QueryStringSingle userRow $"""
        SELECT id, name, email
        FROM {tableName}
        WHERE id = {userId}
        """
```

`SqlServer.unsafe` splices raw, unparameterized SQL — the caller takes responsibility for safety.

---

Built by [Immersus Machina](https://www.immersus-machina.com)
