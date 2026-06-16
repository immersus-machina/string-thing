module StringThing.FSharp.MySql.Analyzers.SupportedParameterTypeAnalyzer

open FSharp.Analyzers.SDK
open StringThing.FSharp.Analyzers.Core

/// Provider configuration for MySQL via MySqlConnector.
let private mySqlConfig: ProviderConfig =
    { ProviderAssemblyName = "StringThing.FSharp.MySql"
      FactoryTypeFullName = "StringThing.FSharp.MySql.MySql"
      FactoryQualifier = "MySql"
      DisplayName = "MySql"
      SupportedScalarFullNames =
        Set.ofList [
            "System.Boolean"
            "System.Int32"
            "System.Int64"
            "System.Double"
            "System.Decimal"
            "System.String"
            "System.Guid"
            "System.DateTime"
            "System.DateTimeOffset"
            "System.TimeSpan"
            "System.Object"
        ] }

/// Reports unsupported parameter shapes in StringThing.FSharp.MySql interpolated SQL.
///
/// Diagnostic codes:
/// * `ST-FS-001` — unsupported value type in a `$""` hole.
/// * `ST-FS-002` — a `FormattableString`-typed hole that isn't itself an inline `$""` or a
///   `SqlFragment` value. Construct one via `MySql.fragment $"..."` instead.
/// * `ST-FS-003` — the FormattableString argument at a connection method can't be traced back
///   to an inline `$""` or a `SqlStatement`/`SqlFragment` value.
[<CliAnalyzer "StringThingFSharpMySql.SupportedParameterType">]
let supportedParameterTypeAnalyzer: Analyzer<CliContext> =
    AnalyzerCore.analyze mySqlConfig
