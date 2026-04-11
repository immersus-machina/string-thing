# StringThing.MySql

Injection-safe interpolated SQL for MySQL/MariaDB with type-checked parameterization, built on MySqlConnector. Part of [StringThing](https://github.com/immersus-machina/string-thing).

## Quick start

```csharp
var userId = 42;
MySql stmt = $"SELECT name FROM users WHERE id = {userId}";
await using var command = stmt.ToCommand(connection);
```

Parameters are named automatically using the variable name: `@userId`, not `@p0`.

## Supported types

`bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `string`, `char`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `byte[]`.

Nullable reference types (`string?`, `byte[]?`) map to `NULL`.

## Fragments

```csharp
var minAge = 18;
var status = "active";
MySqlFragment filter = $"age >= {minAge} AND status = {status}";

MySql stmt = $"SELECT * FROM users WHERE {filter}";
```

## Multi-row insert

```csharp
record InsertUser(int Id, string Name, string? Email) : IMySqlRow
{
    public MySqlFragment RowValues => $"({Id}, {Name}, {Email})";
}

var users = new InsertUser[] { new(1, "alice", "alice@example.com"), new(2, "bob", null) };
MySql stmt = $"INSERT INTO users (id, name, email) VALUES {MySql.InsertRows(users)}";
```

## IN list

```csharp
var ids = new List<int> { 1, 2, 3 };
MySql stmt = $"SELECT * FROM users WHERE id IN {MySql.InList([.. ids])}";
```

## JSON

Implement `IMySqlJson` on your types to store them as JSON. Use your choice of serializer:

```csharp
record UserData(string Name, int Age) : IMySqlJson
{
    public string ToJson() => JsonSerializer.Serialize(this);
}

MySql stmt = $"INSERT INTO data (payload) VALUES ({userData})";
```

## Type overrides

```csharp
// TEXT instead of default VARCHAR
MySql stmt = $"WHERE description = {MySql.Text(description)}";

// TIMESTAMP instead of default DATETIME
MySql stmt = $"WHERE created < {MySql.Timestamp(date)}";
```

## Unsafe escape hatch

```csharp
using StringThing.UnsafeSql;

var tableName = Sql.Unsafe("users");
MySql stmt = $"SELECT * FROM {tableName} WHERE id = {userId}";
```
