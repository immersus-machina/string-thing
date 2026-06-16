# StringThing.FSharp.SqlClient.Analyzers

F# analyzers for [StringThing.FSharp.SqlClient](https://github.com/immersus-machina/string-thing/tree/main/fsharp/src/StringThing.FSharp.SqlClient). Supplies the SQL Server `ProviderConfig` to the shared [`StringThing.FSharp.Analyzers.Core`](../StringThing.FSharp.Analyzers.Core/) library.

**Not a standalone package.** The compiled DLL is bundled inside the `StringThing.FSharp.SqlClient` NuGet package under `analyzers/dotnet/fs/`. Installing the SQL Server provider activates the analyzer automatically.

## What it catches

Same three diagnostics as the SQLite analyzer (`ST-FS-001` / `002` / `003`), driven by the shared `AnalyzerCore`. The SQL Server config adds `decimal`, `DateTimeOffset`, and `TimeSpan` to the scalar allowlist.

See [`StringThing.FSharp.Sqlite.Analyzers`](../StringThing.FSharp.Sqlite.Analyzers/) for full diagnostic documentation — the rules are identical, only the provider's name and supported scalars differ.

---

Built by [Immersus Machina](https://www.immersus-machina.com)
