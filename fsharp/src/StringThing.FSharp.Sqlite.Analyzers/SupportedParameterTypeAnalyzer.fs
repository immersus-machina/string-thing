module StringThing.FSharp.Sqlite.Analyzers.SupportedParameterTypeAnalyzer

open FSharp.Analyzers.SDK
open StringThing.FSharp.Analyzers.Core

/// Provider configuration for SQLite via Microsoft.Data.Sqlite.
let private sqliteConfig: ProviderConfig =
    { ProviderAssemblyName = "StringThing.FSharp.Sqlite"
      FactoryTypeFullName = "StringThing.FSharp.Sqlite.Sqlite"
      FactoryQualifier = "Sqlite"
      DisplayName = "Sqlite"
      SupportedScalarFullNames =
        Set.ofList [
            "System.Boolean"
            "System.Int32"
            "System.Int64"
            "System.Double"
            "System.String"
            "System.Guid"
            "System.DateTime"
            "System.Object"
        ] }

/// Reports unsupported parameter shapes in StringThing.FSharp.Sqlite interpolated SQL.
///
/// Diagnostic codes:
/// * `ST-FS-001` — unsupported value type in a `$""` hole.
/// * `ST-FS-002` — a `FormattableString`-typed hole that isn't itself an inline `$""` or a
///   `SqlFragment` value. Construct one via `Sqlite.fragment $"..."` instead.
/// * `ST-FS-003` — the FormattableString argument at a connection method can't be traced back
///   to an inline `$""` or a `SqlStatement`/`SqlFragment` value.
[<CliAnalyzer "StringThingFSharpSqlite.SupportedParameterType">]
let supportedParameterTypeAnalyzer: Analyzer<CliContext> =
    AnalyzerCore.analyze sqliteConfig
