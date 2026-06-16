module StringThing.FSharp.Npgsql.Analyzers.SupportedParameterTypeAnalyzer

open FSharp.Analyzers.SDK
open StringThing.FSharp.Analyzers.Core

/// Provider configuration for PostgreSQL via Npgsql.
let private postgresConfig: ProviderConfig =
    { ProviderAssemblyName = "StringThing.FSharp.Npgsql"
      FactoryTypeFullName = "StringThing.FSharp.Npgsql.Postgres"
      FactoryQualifier = "Postgres"
      DisplayName = "Postgres"
      SupportedScalarFullNames =
        Set.ofList [
            "System.Boolean"
            "System.Int16"
            "System.Int32"
            "System.Int64"
            "System.Single"
            "System.Double"
            "System.Decimal"
            "System.Char"
            "System.String"
            "System.Guid"
            "System.DateTime"
            "System.DateTimeOffset"
            "System.DateOnly"
            "System.TimeOnly"
            "System.TimeSpan"
            "System.Object"
        ] }

/// Reports unsupported parameter shapes in StringThing.FSharp.Npgsql interpolated SQL.
///
/// Diagnostic codes:
/// * `ST-FS-001` — unsupported value type in a `$""` hole.
/// * `ST-FS-002` — a `FormattableString`-typed hole that isn't itself an inline `$""` or a
///   `SqlFragment` value. Construct one via `Postgres.fragment $"..."` instead.
/// * `ST-FS-003` — the FormattableString argument at a connection method can't be traced back
///   to an inline `$""` or a `SqlStatement`/`SqlFragment` value.
[<CliAnalyzer "StringThingFSharpNpgsql.SupportedParameterType">]
let supportedParameterTypeAnalyzer: Analyzer<CliContext> =
    AnalyzerCore.analyze postgresConfig
