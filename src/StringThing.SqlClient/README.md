# StringThing.SqlClient

Injection-safe interpolated SQL for SQL Server with type-checked parameterization, built on Microsoft.Data.SqlClient. Part of [StringThing](https://github.com/immersus-machina/string-thing).

## Install

```
dotnet add package StringThing.SqlClient
```

## Quick start

```csharp
var userId = 42;
SqlServerSql stmt = $"SELECT name FROM users WHERE id = {userId}";
await using var command = stmt.ToCommand(connection);
```

Parameters are named automatically using the variable name: `@userId`, not `@p0`.

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

SqlServerSql stmt = $"SELECT * FROM users WHERE {filter}";
```

## Multi-row insert

```csharp
record InsertUser(int Id, string Name, string? Email) : ISqlServerRow
{
    public SqlServerFragment RowValues => $"({Id}, {Name}, {Email})";
}

var users = new InsertUser[] { new(1, "alice", "alice@example.com"), new(2, "bob", null) };
SqlServerSql stmt = $"INSERT INTO users (id, name, email) VALUES {SqlServerSql.InsertRows(users)}";
```

## IN list

```csharp
var ids = new List<int> { 1, 2, 3 };
SqlServerSql stmt = $"SELECT * FROM users WHERE id IN {SqlServerSql.InList([.. ids])}";
// produces: SELECT * FROM users WHERE id IN (@p0, @p1, @p2)
```

## Table-Valued Parameters

```csharp
var table = new DataTable();
table.Columns.Add("id", typeof(int));
table.Columns.Add("name", typeof(string));
table.Rows.Add(1, "alice");
table.Rows.Add(2, "bob");

SqlServerSql stmt = $"INSERT INTO users SELECT * FROM {SqlServerSql.Table(table, "dbo.UserTableType")}";
```

Requires a matching type defined on the server (`CREATE TYPE dbo.UserTableType AS TABLE (...)`).

## Type overrides

```csharp
// VarChar instead of default NVarChar
SqlServerSql stmt = $"WHERE code = {SqlServerSql.VarChar(code)}";

// Legacy DateTime instead of default DateTime2
SqlServerSql stmt = $"WHERE created < {SqlServerSql.DateTime(date)}";
```

## Unsafe escape hatch

```csharp
var tableName = Sql.Unsafe("users");
SqlServerSql stmt = $"SELECT * FROM {tableName} WHERE id = {userId}";
```
