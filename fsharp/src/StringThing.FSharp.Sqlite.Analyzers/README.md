# StringThing.FSharp.Sqlite.Analyzers

F# analyzers for [StringThing.FSharp.Sqlite](https://github.com/immersus-machina/string-thing/tree/main/fsharp/src/StringThing.FSharp.Sqlite). Recovers compile-time safety that `FormattableString`-based interpolation can't provide on its own.

**Not a standalone package.** This project's compiled DLL is bundled inside the `StringThing.FSharp.Sqlite` NuGet package under `analyzers/dotnet/fs/`. Installing the Sqlite provider activates the analyzer automatically; there's no separate `dotnet add package` step.

The project supplies the SQLite-specific `ProviderConfig` (assembly name, factory type, supported scalars) to the provider-agnostic [`StringThing.FSharp.Analyzers.Core`](../StringThing.FSharp.Analyzers.Core/) library. The other providers — MySQL, SQL Server, PostgreSQL — each have their own `.Analyzers` project bundled the same way; the rules and diagnostic codes documented here apply identically across all of them, only the provider-specific name (`Sqlite` / `MySql` / `SqlServer` / `Postgres`) and supported scalar set differ.

## What it catches

Three diagnostics, all `Severity.Error`:

### `ST-FS-001` — unsupported parameter type in a hole

Fires on any `$""` literal where a hole's value type isn't in the runtime dispatch allowlist (`bool`, `int`, `int64`, `float`, `string`, `byte[]`, `Guid`, `DateTime`, `SqlFragment`, `obj`, and `Option<T>` of any of the above).

```fsharp
let addr = System.Net.IPAddress.Loopback
connection.ExecuteString $"INSERT INTO requests VALUES ({addr})"
//                                                     ~~~~~~  ST-FS-001
```

### `ST-FS-002` — raw `FormattableString` spliced into a hole

Fires when a hole contains a `FormattableString`-typed value that isn't itself an inline `$""` or a `SqlFragment` value. Use `Sqlite.fragment $"..."` to compose.

```fsharp
let filter : FormattableString = $"id > 1"
connection.QueryString userRow $"SELECT * FROM users WHERE {filter}"
//                                                          ~~~~~~  ST-FS-002
```

### `ST-FS-003` — untraceable `FormattableString` at a call site

Fires when a connection method's argument can't be traced back to an inline `$""` or a `SqlStatement`/`SqlFragment`. Catches the case where a `FormattableString` arrives from an external function or an opaque source.

```fsharp
let opaque () : FormattableString = $"raw"
let stmt = opaque ()
connection.QueryStringSingle userRow stmt
//                                   ~~~~  ST-FS-003
```

## How it works

The shared `AnalyzerCore.analyze` runs two passes (parameterized on the supplied `ProviderConfig`):

1. **TAST walk** for hole-type checks (`ST-FS-001`, `ST-FS-002`) — walks every `FormattableStringFactory.Create` call in the typed tree and checks each `{value}` hole's static type.

2. **Untyped AST walk** for provenance (`ST-FS-003`) — collects all `let` bindings in the file, then at every call to a connection target method (`QueryString*` / `ExecuteString*`), walks the argument expression recursively. It accepts:
   - inline `$""` (`SynExpr.InterpolatedString`)
   - `<Provider>.statement $"..."` / `<Provider>.fragment $"..."` factory calls (with their argument also being blessed)
   - `Ident` references to local bindings whose RHS is itself blessed (recurse)
   - non-local references (functions, parameters, module values) whose declared return/value type is `SqlStatement<'TParameter>` or `SqlFragment<'TParameter>` — looked up via `FSharpCheckFileResults.GetSymbolUseAtLocation`

The untyped-AST approach is necessary because F# 9's implicit-conversion rewriting moves the wrapper-type → `FormattableString` conversion into the let-binding's RHS in the typed tree, hiding the user's intent from a typed-AST check.

## Running

Built on [`FSharp.Analyzers.SDK`](https://github.com/ionide/FSharp.Analyzers.SDK) version 0.31.0. Runs in:

- **Ionide / VS Code** — automatic; the analyzer DLLs are discovered inside the provider NuGet package
- **Rider** — automatic via Ionide compatibility
- **CI** — via the `fsharp-analyzers` CLI tool (requires the version that matches the SDK)

## Known limitations

- **Conditionals in the RHS** (`let stmt = if x then $"a" else $"b"`) aren't traced — the analyzer will reject. Build conditionals as `SqlFragment` values instead: `let frag = if x then Sqlite.fragment $"a" else Sqlite.fragment $"b"`.
- **Cross-file traces** rely entirely on the helper's declared return type. If you build helpers, type them as `SqlStatement` / `SqlFragment` rather than `FormattableString`.

---

Built by [Immersus Machina](https://www.immersus-machina.com)
