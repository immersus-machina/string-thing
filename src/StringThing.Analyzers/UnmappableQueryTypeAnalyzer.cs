using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StringThing.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnmappableQueryTypeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ST0002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Query result type is not mappable",
        "'{0}' is neither a supported scalar type nor a [StringThingRow] type, so it cannot be materialized from a query result",
        "Correctness",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly ImmutableHashSet<SpecialType> ScalarSpecialTypes = ImmutableHashSet.Create(
        SpecialType.System_Boolean,
        SpecialType.System_Byte,
        SpecialType.System_Int16,
        SpecialType.System_Int32,
        SpecialType.System_Int64,
        SpecialType.System_Single,
        SpecialType.System_Double,
        SpecialType.System_Decimal,
        SpecialType.System_String,
        SpecialType.System_Char,
        SpecialType.System_DateTime);

    private static readonly ImmutableHashSet<string> ScalarMetadataNames = ImmutableHashSet.Create(
        "System.Guid",
        "System.DateTimeOffset",
        "System.DateOnly",
        "System.TimeOnly",
        "System.TimeSpan");

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
        var rowInterface = context.Compilation.GetTypeByMetadataName("StringThing.Aot.IStringThingRow`1");
        if (rowInterface is null)
            return;

        context.RegisterSyntaxNodeAction(
            syntaxNodeContext => AnalyzeInvocation(syntaxNodeContext, rowInterface),
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol rowInterface)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol method)
            return;

        if (!IsResultMappingMethod(method))
            return;

        var rowType = method.TypeArguments[0];

        if (rowType.TypeKind == TypeKind.TypeParameter || rowType.TypeKind == TypeKind.Error)
            return;

        if (IsMappable(rowType, rowInterface))
            return;

        var location = TypeArgumentLocation(invocation) ?? invocation.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, rowType.ToDisplayString()));
    }

    private static bool IsResultMappingMethod(IMethodSymbol method)
    {
        if (!method.IsExtensionMethod || method.TypeArguments.Length != 1)
            return false;

        if (!method.Name.StartsWith("QueryString", System.StringComparison.Ordinal))
            return false;

        return method.ContainingType?.ContainingNamespace?.ToDisplayString()
            .StartsWith("StringThing", System.StringComparison.Ordinal) == true;
    }

    private static bool IsMappable(ITypeSymbol rowType, INamedTypeSymbol rowInterface)
    {
        return IsSupportedScalar(rowType) || ImplementsRowInterface(rowType, rowInterface);
    }

    private static bool IsSupportedScalar(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return IsSupportedScalar(named.TypeArguments[0]);

        if (type is IArrayTypeSymbol array)
            return array.ElementType.SpecialType == SpecialType.System_Byte;

        if (ScalarSpecialTypes.Contains(type.SpecialType))
            return true;

        return ScalarMetadataNames.Contains(type.ToDisplayString());
    }

    private static bool ImplementsRowInterface(ITypeSymbol rowType, INamedTypeSymbol rowInterface)
    {
        return rowType.AllInterfaces.Any(implemented =>
            SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, rowInterface));
    }

    private static Location? TypeArgumentLocation(InvocationExpressionSyntax invocation)
    {
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            SimpleNameSyntax simpleName => simpleName,
            _ => null,
        };

        return name is GenericNameSyntax generic && generic.TypeArgumentList.Arguments.Count > 0
            ? generic.TypeArgumentList.Arguments[0].GetLocation()
            : null;
    }
}
