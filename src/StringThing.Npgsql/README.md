# StringThing.Npgsql

Injection-safe interpolated SQL for PostgreSQL, built on Npgsql. Part of [StringThing](https://github.com/immersus-machina/string-thing).

## Install

```
dotnet add package StringThing.Npgsql
```

## Quick start

```csharp
var userId = 42;
PostgresSql stmt = $"SELECT name FROM users WHERE id = {userId}";
await using var command = stmt.ToCommand(dataSource);
```

## Supported types

All standard .NET types (`int`, `string`, `DateTime`, `Guid`, etc.) plus Postgres-specific types: `NpgsqlPoint`, `NpgsqlBox`, `NpgsqlRange<T>`, `NpgsqlTsVector`, `NpgsqlTsQuery`, `IPAddress`, and more. See [`PostgresValue`](https://github.com/immersus-machina/string-thing/blob/main/src/StringThing.Npgsql/PostgresValue.cs) for the full list.

Nullable variants are supported. Reference types (`string?`, `byte[]?`, `IPAddress?`) map to `NULL`. Value types use `PostgresValue?`.

## Fragments

```csharp
var minAge = 18;
var status = "active";
PostgresFragment filter = $"age >= {minAge} AND status = {status}";

PostgresSql stmt = $"SELECT * FROM users WHERE {filter}";
```

Fragments compose. Parameters renumber automatically across fragment boundaries.

## Multi-row insert

```csharp
record InsertUser(int Id, string Name, string? Email) : IPostgresRow
{
    public PostgresFragment RowValues => $"({Id}, {Name}, {Email})";
}

var users = new InsertUser[] { new(1, "alice", "alice@example.com"), new(2, "bob", null) };
PostgresSql stmt = $"INSERT INTO users (id, name, email) VALUES {PostgresSql.InsertRows(users)}";
```

## JSON

Implement `IPostgresJson` on your types to store them as `jsonb`. Use your choice of serializer:

```csharp
record UserData(string Name, int Age) : IPostgresJson
{
    public string ToJson() => $$"""{"name":"{{Name}}","age":{{Age}}}""";
}

PostgresSql stmt = $"INSERT INTO data (payload) VALUES ({userData})";
```

## Unsafe escape hatch

```csharp
var tableName = Sql.Unsafe("users");
PostgresSql stmt = $"SELECT * FROM {tableName} WHERE id = {userId}";
```
