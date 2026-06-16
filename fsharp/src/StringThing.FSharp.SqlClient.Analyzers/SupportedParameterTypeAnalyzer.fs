module StringThing.FSharp.SqlClient.Analyzers.SupportedParameterTypeAnalyzer

open FSharp.Analyzers.SDK
open StringThing.FSharp.Analyzers.Core

/// Provider configuration for SQL Server via Microsoft.Data.SqlClient.
let private sqlServerConfig: ProviderConfig =
    { ProviderAssemblyName = "StringThing.FSharp.SqlClient"
      FactoryTypeFullName = "StringThing.FSharp.SqlClient.SqlServer"
      FactoryQualifier = "SqlServer"
      DisplayName = "SqlServer"
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

/// Reports unsupported parameter shapes in StringThing.FSharp.SqlClient interpolated SQL.
///
/// Diagnostic codes:
/// * `ST-FS-001` — unsupported value type in a `$""` hole.
/// * `ST-FS-002` — a `FormattableString`-typed hole that isn't itself an inline `$""` or a
///   `SqlFragment` value. Construct one via `SqlServer.fragment $"..."` instead.
/// * `ST-FS-003` — the FormattableString argument at a connection method can't be traced back
///   to an inline `$""` or a `SqlStatement`/`SqlFragment` value.
[<CliAnalyzer "StringThingFSharpSqlClient.SupportedParameterType">]
let supportedParameterTypeAnalyzer: Analyzer<CliContext> =
    AnalyzerCore.analyze sqlServerConfig
