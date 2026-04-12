using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StringThing.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateHandlerPerLineAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ST0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Multiple cached SQL handler invocations on the same source line",
        "This interpolated SQL statement shares a source line with another, causing a cache key collision. Move each statement to a separate line.",
        "Correctness",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var sqlStatementType = context.Compilation.GetTypeByMetadataName("StringThing.Core.SqlStatement`2");
        if (sqlStatementType is null)
            return;

        context.RegisterSemanticModelAction(
            semanticModelContext => AnalyzeFile(semanticModelContext, sqlStatementType));
    }

    private static void AnalyzeFile(
        SemanticModelAnalysisContext context,
        INamedTypeSymbol sqlStatementType)
    {
        var root = context.SemanticModel.SyntaxTree
            .GetRoot(context.CancellationToken);

        var handlersByLine = new Dictionary<int, List<Location>>();

        foreach (var node in root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
        {
            if (context.CancellationToken.IsCancellationRequested)
                return;

            if (!IsHandlerForSqlStatement(context.SemanticModel, node, sqlStatementType, context.CancellationToken))
                continue;

            var location = node.GetLocation();
            var lineSpan = location.GetLineSpan();
            if (!lineSpan.IsValid)
                continue;

            var line = lineSpan.StartLinePosition.Line;
            if (!handlersByLine.TryGetValue(line, out var locations))
                handlersByLine[line] = locations = new List<Location>();
            locations.Add(location);
        }

        foreach (var entry in handlersByLine)
        {
            var locations = entry.Value;
            if (locations.Count <= 1)
                continue;

            locations.Sort((a, b) =>
                a.GetLineSpan().StartLinePosition.Character
                    .CompareTo(b.GetLineSpan().StartLinePosition.Character));

            for (var i = 1; i < locations.Count; i++)
                context.ReportDiagnostic(Diagnostic.Create(Rule, locations[i]));
        }
    }

    private static bool IsHandlerForSqlStatement(
        SemanticModel semanticModel,
        InterpolatedStringExpressionSyntax interpolatedString,
        INamedTypeSymbol sqlStatementType,
        System.Threading.CancellationToken cancellationToken)
    {
        var typeInfo = semanticModel.GetTypeInfo(interpolatedString, cancellationToken);

        var targetType = typeInfo.ConvertedType ?? typeInfo.Type;
        if (targetType is not null && DerivesFrom(targetType, sqlStatementType))
            return true;

        if (interpolatedString.Parent is CastExpressionSyntax castExpression)
        {
            var castTypeInfo = semanticModel.GetTypeInfo(castExpression, cancellationToken);
            targetType = castTypeInfo.Type;
            if (targetType is not null && DerivesFrom(targetType, sqlStatementType))
                return true;
        }

        return false;
    }

    private static bool DerivesFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }
}
