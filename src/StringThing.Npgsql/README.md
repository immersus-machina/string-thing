# StringThing.Npgsql

Injection-safe interpolated SQL for PostgreSQL, built on Npgsql. Part of [StringThing](https://github.com/immersus-machina/string-thing).

## Install

```
dotnet add package StringThing.Npgsql
```

## Quick start

```csharp
var userId = 42;
PostgresSql statement = $"SELECT name FROM users WHERE id = {userId}";
await using var command = statement.ToCommand(dataSource);
```

## Result mapping

Mark row types with `[StringThingRow]` and declare them `partial`. A source generator emits an AOT-friendly row materializer — no reflection, no IL emit, no third-party mapper.

```csharp
using StringThing.Aot;

[StringThingRow]
public partial record User(int Id, string Name, string? Email);

await using var connection = await dataSource.OpenConnectionAsync();

var user = await connection.QueryStringSingleAsync<User>(
    $"SELECT id AS \"Id\", name AS \"Name\", email AS \"Email\" FROM users WHERE id = {userId}");

var users = await connection.QueryStringAsync<User>(
    $"SELECT id AS \"Id\", name AS \"Name\", email AS \"Email\" FROM users ORDER BY id");

await connection.ExecuteStringAsync($"DELETE FROM users WHERE id = {userId}");
```

The full surface: `QueryString<T>`, `QueryStringFirst<T>`, `QueryStringFirstOrDefault<T>`, `QueryStringSingle<T>`, `QueryStringSingleOrDefault<T>`, `ExecuteString`, `ExecuteStringScalar` (+ `T` overload), plus `Async` variants. Column ordinals are resolved once per query; rows are then read by ordinal — name-based binding without per-row name lookup.

Override the column name with `[Column]` from `System.ComponentModel.DataAnnotations.Schema`:

```csharp
[StringThingRow]
public partial record User(
    [property: Column("user_id")] int Id,
    [property: Column("full_name")] string Name);
```

Nullable annotations drive `IsDBNull` checks — `string?` becomes a null-checked read; `string` is a direct read.

If the generator can't handle your shape — say, you want to derive a property from a column rather than read it straight, or read columns into a shape the generator couldn't infer from the type — implement `IStringThingRow<T>` by hand. Same runtime path:

```csharp
public sealed class UserSummary : IStringThingRow<UserSummary>
{
    public int Id { get; init; }
    public string Status { get; init; } = "";

    public static ReadOnlySpan<string> ColumnBindingOrder => ["id", "email"];

    public static UserSummary Read(DbDataReader reader, ReadOnlySpan<int> ordinals) => new()
    {
        Id = reader.GetInt32(ordinals[0]),
        Status = reader.IsDBNull(ordinals[1]) ? "no-email" : "has-email",
    };
}
```

## Supported types

All standard .NET types (`int`, `string`, `DateTime`, `Guid`, etc.) plus Postgres-specific types: `NpgsqlPoint`, `NpgsqlBox`, `NpgsqlRange<T>`, `NpgsqlTsVector`, `NpgsqlTsQuery`, `IPAddress`, and more. See [`PostgresValue`](https://github.com/immersus-machina/string-thing/blob/main/src/StringThing.Npgsql/PostgresValue.cs) for the full list.

Nullable variants are supported. Reference types (`string?`, `byte[]?`, `IPAddress?`) map to `NULL`. Value types use `PostgresValue?`.

## Fragments

```csharp
var minAge = 18;
var status = "active";
PostgresFragment filter = $"age >= {minAge} AND status = {status}";

PostgresSql statement = $"SELECT * FROM users WHERE {filter}";
```

Fragments compose. Parameters renumber automatically across fragment boundaries.

## Multi-row insert

```csharp
record InsertUser(int Id, string Name, string? Email) : IPostgresRow
{
    public PostgresFragment RowValues => $"({Id}, {Name}, {Email})";
}

var users = new InsertUser[] { new(1, "alice", "alice@example.com"), new(2, "bob", null) };
PostgresSql statement = $"INSERT INTO users (id, name, email) VALUES {PostgresSql.InsertRows(users)}";
```

## JSON

Implement `IPostgresJson` on your types to store them as `jsonb`. Use your choice of serializer:

```csharp
record UserData(string Name, int Age) : IPostgresJson
{
    public string ToJson() => $$"""{"name":"{{Name}}","age":{{Age}}}""";
}

PostgresSql statement = $"INSERT INTO data (payload) VALUES ({userData})";
```

## Unsafe escape hatch

```csharp
var tableName = Sql.Unsafe("users");
PostgresSql statement = $"SELECT * FROM {tableName} WHERE id = {userId}";
```

---

Built by [Immersus Machina](https://www.immersus-machina.com)
