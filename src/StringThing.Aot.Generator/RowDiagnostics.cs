using Microsoft.CodeAnalysis;

namespace StringThing.Aot.Generator;

internal static class RowDiagnostics
{
    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "STAOT001",
        title: "Type marked [StringThingRow] must be partial",
        messageFormat: "Type '{0}' is marked with [StringThingRow] but is not declared 'partial'. The generator cannot emit the row implementation.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoConstructionStrategy = new(
        id: "STAOT002",
        title: "Type marked [StringThingRow] has no usable construction strategy",
        messageFormat: "Type '{0}' has no usable construction strategy. Provide a single public constructor whose parameters match readable members, or a parameterless constructor plus settable properties.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedTypesUnsupported = new(
        id: "STAOT003",
        title: "Nested types are not supported by [StringThingRow]",
        messageFormat: "Type '{0}' is nested inside another type. [StringThingRow] currently supports only top-level types within a namespace.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
