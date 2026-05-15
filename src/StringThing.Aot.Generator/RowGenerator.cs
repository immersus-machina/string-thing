using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StringThing.Aot.Generator;

[Generator]
public sealed class RowGenerator : IIncrementalGenerator
{
    private const string MarkerAttributeMetadataName = "StringThing.Aot.StringThingRowAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var rowTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            MarkerAttributeMetadataName,
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (syntaxContext, cancellationToken) =>
                RowModelFactory.Build(syntaxContext, cancellationToken));

        context.RegisterSourceOutput(rowTypes, EmitForType);
    }

    private static void EmitForType(SourceProductionContext context, RowModelResult result)
    {
        foreach (var diagnostic in result.Diagnostics)
            context.ReportDiagnostic(diagnostic);

        if (result.Model is null)
            return;

        var source = RowSourceWriter.Write(result.Model);
        context.AddSource($"{result.Model.HintName}.g.cs", source);
    }
}

internal readonly record struct RowModelResult(RowModel? Model, ImmutableArray<Diagnostic> Diagnostics)
{
    public static RowModelResult Success(RowModel model) =>
        new(model, ImmutableArray<Diagnostic>.Empty);

    public static RowModelResult Failure(ImmutableArray<Diagnostic> diagnostics) =>
        new(null, diagnostics);
}
