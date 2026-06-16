namespace StringThing.FSharp.Analyzers.Core

/// Per-provider configuration consumed by the shared analyzer logic. Each provider
/// (Sqlite, Postgres, SqlClient, MySql, ...) supplies one of these and reuses
/// `AnalyzerCore.analyze` to produce its analyzer.
type ProviderConfig = {
    /// Assembly name of the provider library. Used to detect calls to the provider's
    /// connection extension methods (e.g. `SqliteConnection.QueryStringSingle`).
    /// Example: `"StringThing.FSharp.Sqlite"`.
    ProviderAssemblyName: string

    /// Full name of the provider's factory type. Used to confirm that calls to
    /// `Sqlite.statement` / `Sqlite.fragment` actually originate from the provider's
    /// factory and not a lookalike.
    /// Example: `"StringThing.FSharp.Sqlite.Sqlite"`.
    FactoryTypeFullName: string

    /// Short qualifier used in syntactic factory detection. The analyzer matches
    /// `[Qualifier; "statement"]` / `[Qualifier; "fragment"]` chains in the source.
    /// Example: `"Sqlite"` so `Sqlite.statement $"..."` is detected.
    FactoryQualifier: string

    /// Set of fully-qualified scalar type names this provider's runtime dispatch
    /// can handle. `System.Object` should be included so that values returned by
    /// `unsafe` / `inList` / `insertRows` (which return `obj`) pass the hole check.
    SupportedScalarFullNames: Set<string>

    /// Display name for the provider used in diagnostic messages.
    /// Example: `"Sqlite"`.
    DisplayName: string
}
