# StringThing.FSharp.Analyzers.Core

Provider-agnostic analyzer logic for the StringThing.FSharp family.

**Not a standalone package.** The compiled DLL is bundled inside each provider's NuGet package alongside the per-provider analyzer DLL (in `analyzers/dotnet/fs/`). Consumers don't reference this project directly — they get it transitively by installing a provider package like `StringThing.FSharp.Sqlite`.

## What's here

- `ProviderConfig` — record type with the per-provider knobs: assembly name, factory type, factory qualifier, supported scalar set, display name.
- `AnalyzerCore.analyze` — the actual analyzer. Takes a `ProviderConfig` and a `CliContext` and returns the message list. Runs three diagnostics (`ST-FS-001` / `002` / `003`), uses a hybrid TAST + untyped-AST walker, handles let-binding tracing and symbol type lookup.

## Why this exists

The same analyzer rules apply to every provider — only the **provider-specific identifiers** differ:

| Differs | Same |
|---|---|
| provider assembly name (`"StringThing.FSharp.Sqlite"` vs `"…MySql"` vs `…`) | hole-type checking |
| factory full name (`"StringThing.FSharp.Sqlite.Sqlite"` vs `…`) | binding trace + symbol resolution |
| factory qualifier (`"Sqlite"` for syntactic `Sqlite.statement` detection) | wrapper-type detection (uses shared generic types from `StringThing.FSharp`) |
| supported scalar allowlist (each driver's range of typed parameters) | diagnostic codes and message shapes |

Per-provider analyzers shrink to ~38 lines: a `ProviderConfig` literal plus a one-line entry point routing to `AnalyzerCore.analyze`. See [`StringThing.FSharp.Sqlite.Analyzers`](../StringThing.FSharp.Sqlite.Analyzers/) for the canonical example.

## When to read the code

If you want to understand how the analyzer actually works — the binding-trace walker, the symbol-type lookup, why hybrid TAST + untyped AST is necessary — `AnalyzerCore.fs` is where it lives.

---

Built by [Immersus Machina](https://www.immersus-machina.com)
