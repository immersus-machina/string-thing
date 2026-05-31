using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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

        var handWrittenRowTypes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is TypeDeclarationSyntax type && MentionsRowInterface(type),
            transform: static (syntaxContext, cancellationToken) =>
                TryBuildHandWrittenRegistrar(syntaxContext, cancellationToken))
            .Where(static model => model is not null);

        context.RegisterSourceOutput(handWrittenRowTypes, EmitRegistrar);
    }

    private static bool MentionsRowInterface(TypeDeclarationSyntax type)
    {
        if (type.BaseList is null)
            return false;

        foreach (var baseType in type.BaseList.Types)
        {
            if (RightmostName(baseType.Type) == "IStringThingRow")
                return true;
        }
        return false;
    }

    private static string? RightmostName(TypeSyntax type) => type switch
    {
        GenericNameSyntax generic => generic.Identifier.Text,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        _ => null,
    };

    private static RegistrarModel? TryBuildHandWrittenRegistrar(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol symbol)
            return null;

        if (symbol.ContainingType is not null)
            return null;

        if (symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == MarkerAttributeMetadataName))
            return null;

        if (!ImplementsRowInterface(symbol))
            return null;

        var namespaceName = symbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        var fullyQualifiedTypeName = namespaceName.Length == 0
            ? symbol.Name
            : $"{namespaceName}.{symbol.Name}";

        return new RegistrarModel(namespaceName, symbol.Name, fullyQualifiedTypeName, fullyQualifiedTypeName);
    }

    private static bool ImplementsRowInterface(INamedTypeSymbol symbol)
    {
        return symbol.AllInterfaces.Any(implemented =>
            implemented.Name == "IStringThingRow" &&
            implemented.TypeArguments.Length == 1 &&
            implemented.ContainingNamespace?.ToDisplayString() == "StringThing.Aot");
    }

    private static void EmitRegistrar(SourceProductionContext context, RegistrarModel? model)
    {
        if (model is null)
            return;

        context.AddSource($"{model.HintName}.Registrar.g.cs", RowSourceWriter.WriteRegistrarOnly(model));
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
