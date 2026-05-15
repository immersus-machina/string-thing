# StringThing.Sqlite

Injection-safe interpolated SQL for SQLite, built on Microsoft.Data.Sqlite. Part of [StringThing](https://github.com/immersus-machina/string-thing).

SQLite is dynamically typed — the compile-time type contract here is light. The value is injection safety by construction, parameter deduplication, and composable fragments.

## Install

```
dotnet add package StringThing.Sqlite
```

## Quick start

```csharp
var userId = 42;
SqliteSql stmt = $"SELECT name FROM users WHERE id = {userId}";
using var command = stmt.ToCommand(connection);
```

Parameters are named automatically using the variable name: `@userId`, not `@p0`.

No container or server needed — SQLite is embedded.

## Result mapping

Mark row types with `[StringThingRow]` and declare them `partial`. A source generator emits an AOT-friendly row materializer — no reflection, no IL emit, no third-party mapper.

```csharp
using StringThing.Aot;

[StringThingRow]
public partial record User(long Id, string Name, string? Email);

var user = connection.QueryStringSingle<User>(
    $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}");

var users = connection.QueryString<User>(
    $"SELECT id AS Id, name AS Name, email AS Email FROM users ORDER BY id");

connection.ExecuteString(
    $"DELETE FROM users WHERE id = {userId}");
```

The full surface: `QueryString<T>`, `QueryStringFirst<T>`, `QueryStringFirstOrDefault<T>`, `QueryStringSingle<T>`, `QueryStringSingleOrDefault<T>`, `ExecuteString`, `ExecuteStringScalar` (+ `T` overload), plus `Async` variants. Column ordinals are resolved once per query; rows are then read by ordinal — name-based binding without per-row name lookup.

Override the column name with `[Column]` from `System.ComponentModel.DataAnnotations.Schema`:

```csharp
[StringThingRow]
public partial record User(
    [property: Column("user_id")] long Id,
    [property: Column("full_name")] string Name);
```

Nullable annotations drive `IsDBNull` checks — `string?` becomes a null-checked read; `string` is a direct read.

If the generator can't handle your shape — say, you want to derive a property from a column rather than read it straight, or read columns into a shape the generator couldn't infer from the type — implement `IStringThingRow<T>` by hand. Same runtime path:

```csharp
public sealed class UserSummary : IStringThingRow<UserSummary>
{
    public long Id { get; init; }
    public string Status { get; init; } = "";

    public static ReadOnlySpan<string> ColumnBindingOrder => ["id", "email"];

    public static UserSummary Read(DbDataReader reader, ReadOnlySpan<int> ordinals) => new()
    {
        Id = reader.GetInt64(ordinals[0]),
        Status = reader.IsDBNull(ordinals[1]) ? "no-email" : "has-email",
    };
}
```

## Supported types

`bool`, `byte`, `short`, `int`, `long`, `float`, `double`, `decimal`, `string`, `char`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `byte[]`.

SQLite is dynamically typed — values are stored as TEXT, INTEGER, REAL, BLOB, or NULL regardless of the declared column type. StringThing ensures the .NET value reaches SQLite correctly.

Nullable reference types (`string?`, `byte[]?`) map to `NULL`.

## Fragments

```csharp
var minAge = 18;
var status = "active";
SqliteFragment filter = $"age >= {minAge} AND status = {status}";

SqliteSql stmt = $"SELECT * FROM users WHERE {filter}";
```

## Multi-row insert

```csharp
record InsertUser(int Id, string Name, string? Email) : ISqliteRow
{
    public SqliteFragment RowValues => $"({Id}, {Name}, {Email})";
}

var users = new InsertUser[] { new(1, "alice", "alice@example.com"), new(2, "bob", null) };
SqliteSql stmt = $"INSERT INTO users (id, name, email) VALUES {SqliteSql.InsertRows(users)}";
```

## IN list

```csharp
var ids = new List<int> { 1, 2, 3 };
SqliteSql stmt = $"SELECT * FROM users WHERE id IN {SqliteSql.InList([.. ids])}";
```

## Unsafe escape hatch

```csharp
using StringThing.UnsafeSql;

var tableName = Sql.Unsafe("users");
SqliteSql stmt = $"SELECT * FROM {tableName} WHERE id = {userId}";
```

---

Built by [Immersus Machina](https://www.immersus-machina.com)
