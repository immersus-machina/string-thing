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
