# StringThing.SqlClient

Injection-safe interpolated SQL for SQL Server with type-checked parameterization, built on Microsoft.Data.SqlClient. Part of [StringThing](https://github.com/immersus-machina/string-thing).

## Install

```
dotnet add package StringThing.SqlClient
```

## Quick start

```csharp
var userId = 42;
SqlServerSql statement = $"SELECT name FROM users WHERE id = {userId}";
await using var command = statement.ToCommand(connection);
```

Parameters are named automatically using the variable name: `@userId`, not `@p0`.

## Result mapping

Mark row types with `[StringThingRow]` and declare them `partial`. A source generator emits an AOT-friendly row materializer — no reflection, no IL emit, no third-party mapper.

```csharp
using StringThing.Aot;

[StringThingRow]
public partial record User(int Id, string Name, string? Email);

var user = await connection.QueryStringSingleAsync<User>(
    $"SELECT id AS Id, name AS Name, email AS Email FROM users WHERE id = {userId}");

var users = await connection.QueryStringAsync<User>(
    $"SELECT id AS Id, name AS Name, email AS Email FROM users ORDER BY id");

await connection.ExecuteStringAsync(
    $"DELETE FROM users WHERE id = {userId}");
```

The full surface: `QueryString<T>`, `QueryStringFirst<T>`, `QueryStringFirstOrDefault<T>`, `QueryStringSingle<T>`, `QueryStringSingleOrDefault<T>`, `ExecuteString`, `ExecuteStringScalar` (+ `T` overload), plus `Async` variants. Column ordinals are resolved once per query; rows are then read by ordinal — name-based binding without per-row name lookup.

Scalar columns map directly. When `T` is a supported scalar type rather than a `[StringThingRow]` type, the query reads its first column into that value — no row type or wrapper needed:

```csharp
var ids = await connection.QueryStringAsync<int>($"SELECT id FROM users ORDER BY id");
var name = await connection.QueryStringSingleAsync<string>($"SELECT name FROM users WHERE id = {id}");
var email = await connection.QueryStringSingleAsync<string?>($"SELECT email FROM users WHERE id = {id}");
```

Nullable scalars read `NULL` as `null`/`default`. A `T` that is neither a supported scalar nor a `[StringThingRow]` type is a compile error (`ST0002`).

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

`bool`, `byte`, `short`, `int`, `long`, `float`, `double`, `decimal`, `string`, `char`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `byte[]`.

Nullable reference types (`string?`, `byte[]?`) map to `NULL`.

## Named parameters

StringThing uses `CallerArgumentExpression` to produce readable parameter names:

```csharp
var userId = 42;
var name = "alice";
// produces: WHERE id = @userId AND name = @name
```

Member access uses underscores: `{user.Id}` becomes `@user_Id`.

Falls back to `@p0`, `@p1` for inline literals and function calls.

Variable names containing underscores (`user_id`), matching the pattern `p{digits}` (`p3`), or containing non-ASCII characters will fall back to indexed naming (`@p0`).

## Fragments

```csharp
var minAge = 18;
var status = "active";
SqlServerFragment filter = $"age >= {minAge} AND status = {status}";

SqlServerSql statement = $"SELECT * FROM users WHERE {filter}";
```

## Multi-row insert

```csharp
record InsertUser(int Id, string Name, string? Email) : ISqlServerRow
{
    public SqlServerFragment RowValues => $"({Id}, {Name}, {Email})";
}

var users = new InsertUser[] { new(1, "alice", "alice@example.com"), new(2, "bob", null) };
SqlServerSql statement = $"INSERT INTO users (id, name, email) VALUES {SqlServerSql.InsertRows(users)}";
```

## IN list

```csharp
var ids = new List<int> { 1, 2, 3 };
SqlServerSql statement = $"SELECT * FROM users WHERE id IN {SqlServerSql.InList([.. ids])}";
// produces: SELECT * FROM users WHERE id IN (@p0, @p1, @p2)
```

## Table-Valued Parameters

```csharp
var table = new DataTable();
table.Columns.Add("id", typeof(int));
table.Columns.Add("name", typeof(string));
table.Rows.Add(1, "alice");
table.Rows.Add(2, "bob");

SqlServerSql statement = $"INSERT INTO users SELECT * FROM {SqlServerSql.Table(table, "dbo.UserTableType")}";
```

Requires a matching type defined on the server (`CREATE TYPE dbo.UserTableType AS TABLE (...)`).

## Type overrides

```csharp
// VarChar instead of default NVarChar
SqlServerSql statement = $"WHERE code = {SqlServerSql.VarChar(code)}";

// Legacy DateTime instead of default DateTime2
SqlServerSql statement = $"WHERE created < {SqlServerSql.DateTime(date)}";
```

## Unsafe escape hatch

```csharp
var tableName = Sql.Unsafe("users");
SqlServerSql statement = $"SELECT * FROM {tableName} WHERE id = {userId}";
```

---

Built by [Immersus Machina](https://www.immersus-machina.com)
