# StringThing.Sqlite.Dapper

Dapper result mapping for [StringThing.Sqlite](https://github.com/immersus-machina/string-thing). Injection-safe interpolated SQL on the input side, Dapper mapping on the output side.

## Install

```
dotnet add package StringThing.Sqlite.Dapper
```

## Quick start

```csharp
var userId = 42;
var user = connection.QueryStringSingle<User>(
    $"SELECT id, name, email FROM users WHERE id = {userId}");
```

No container needed — SQLite is embedded. Works with in-memory databases.

## Available methods

All methods are extension methods on `SqliteConnection`:

| Method | Returns |
|--------|---------|
| `QueryStringAsync<T>` | `Task<List<T>>` |
| `QueryStringFirstAsync<T>` | `Task<T>` |
| `QueryStringFirstOrDefaultAsync<T>` | `Task<T?>` |
| `QueryStringSingleAsync<T>` | `Task<T>` |
| `QueryStringSingleOrDefaultAsync<T>` | `Task<T?>` |
| `ExecuteStringAsync` | `Task<int>` |
| `ExecuteStringScalarAsync<T>` | `Task<T?>` |

Synchronous variants are also available (without the `Async` suffix).

## Why "String" in the method names?

The method names include `String` to avoid collision with Dapper's own extension methods. If both were named `QueryAsync`, a `using` change could silently switch from the safe StringThing path to Dapper's string-based path — defeating the injection safety guarantee.

## Dapper license

This package bundles [Dapper](https://github.com/DapperLib/Dapper) internally for result mapping. Dapper is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0), copyright 2019 Stack Exchange, Inc. The bundled Dapper DLL is not exposed as a dependency — consumers do not need a separate Dapper reference.
