# StringThing.FSharp.Npgsql.Analyzers

F# analyzers for [StringThing.FSharp.Npgsql](https://github.com/immersus-machina/string-thing/tree/main/fsharp/src/StringThing.FSharp.Npgsql). Supplies the PostgreSQL `ProviderConfig` to the shared [`StringThing.FSharp.Analyzers.Core`](../StringThing.FSharp.Analyzers.Core/) library.

**Not a standalone package.** The compiled DLL is bundled inside the `StringThing.FSharp.Npgsql` NuGet package under `analyzers/dotnet/fs/`. Installing the PostgreSQL provider activates the analyzer automatically.

## What it catches

Same three diagnostics as the SQLite analyzer (`ST-FS-001` / `002` / `003`), driven by the shared `AnalyzerCore`. The Postgres config adds the broader scalar allowlist (`int16`, `float32`, `char`, `DateOnly`, `TimeOnly`).

See [`StringThing.FSharp.Sqlite.Analyzers`](../StringThing.FSharp.Sqlite.Analyzers/) for full diagnostic documentation — the rules are identical, only the provider's name and supported scalars differ.

---

Built by [Immersus Machina](https://www.immersus-machina.com)
