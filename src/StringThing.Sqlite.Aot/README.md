# StringThing.Sqlite.Aot

AOT-compatible result mapping for [StringThing.Sqlite](https://www.nuget.org/packages/StringThing.Sqlite).

Source-generated row materializers — no runtime reflection, no IL emit, no Dapper dependency. Compatible with `PublishAot`.

## Usage

Mark your row type with `[StringThingRow]` and declare it `partial`:

```csharp
using StringThing.Aot;

[StringThingRow]
public partial record User(int Id, string Name, string? Email);
```

Query as you would with the `.Dapper` companion:

```csharp
using StringThing.Sqlite;
using StringThing.Sqlite.Aot;

SqliteSql statement = $"SELECT id, name, email FROM users WHERE id = {userId}";
var user = connection.QueryStringSingle<User>(statement);
```

The source generator emits the row materializer at compile time. Column names map to property names by default; override with `[Column("user_name")]` from `System.ComponentModel.DataAnnotations.Schema`.
