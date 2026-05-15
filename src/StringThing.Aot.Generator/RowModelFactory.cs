using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StringThing.Aot.Generator;

internal static class RowModelFactory
{
    private const string ColumnAttributeMetadataName = "System.ComponentModel.DataAnnotations.Schema.ColumnAttribute";

    public static RowModelResult Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var typeSyntax = (TypeDeclarationSyntax)context.TargetNode;
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        if (typeSymbol.ContainingType is not null)
        {
            diagnostics.Add(Diagnostic.Create(
                RowDiagnostics.NestedTypesUnsupported,
                typeSyntax.Identifier.GetLocation(),
                typeSymbol.Name));
            return RowModelResult.Failure(diagnostics.ToImmutable());
        }

        if (!IsPartial(typeSyntax))
        {
            diagnostics.Add(Diagnostic.Create(
                RowDiagnostics.MustBePartial,
                typeSyntax.Identifier.GetLocation(),
                typeSymbol.Name));
            return RowModelResult.Failure(diagnostics.ToImmutable());
        }

        var construction = ResolveConstruction(typeSymbol, out var columns);
        if (construction is null)
        {
            diagnostics.Add(Diagnostic.Create(
                RowDiagnostics.NoConstructionStrategy,
                typeSyntax.Identifier.GetLocation(),
                typeSymbol.Name));
            return RowModelResult.Failure(diagnostics.ToImmutable());
        }

        var typeKindKeyword = ResolveTypeKindKeyword(typeSyntax, typeSymbol);
        var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();

        var hintName = namespaceName.Length == 0
            ? typeSymbol.Name
            : $"{namespaceName}.{typeSymbol.Name}";

        var model = new RowModel
        {
            Namespace = namespaceName,
            TypeName = typeSymbol.Name,
            TypeKindKeyword = typeKindKeyword,
            IsRecord = typeSymbol.IsRecord,
            Construction = construction.Value,
            Columns = columns,
            HintName = hintName,
        };

        return RowModelResult.Success(model);
    }

    private static bool IsPartial(TypeDeclarationSyntax syntax)
    {
        foreach (var modifier in syntax.Modifiers)
        {
            if (modifier.ValueText == "partial")
                return true;
        }
        return false;
    }

    private static string ResolveTypeKindKeyword(TypeDeclarationSyntax syntax, INamedTypeSymbol symbol)
    {
        if (symbol.IsRecord)
            return symbol.TypeKind == TypeKind.Struct ? "record struct" : "record";
        return symbol.TypeKind == TypeKind.Struct ? "struct" : "class";
    }

    private static ConstructionStrategy? ResolveConstruction(
        INamedTypeSymbol typeSymbol,
        out IReadOnlyList<RowColumn> columns)
    {
        var settableProperties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .Where(p => !p.IsStatic && !p.IsReadOnly)
            .Where(p => p.SetMethod is not null && p.SetMethod.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        var primaryCtor = SelectPrimaryConstructor(typeSymbol);
        if (primaryCtor is not null)
        {
            var ctorColumns = new List<RowColumn>(primaryCtor.Parameters.Length);
            foreach (var parameter in primaryCtor.Parameters)
            {
                var matchingProperty = FindPropertyForParameter(typeSymbol, parameter);
                ctorColumns.Add(new RowColumn
                {
                    MemberName = parameter.Name,
                    ColumnName = ResolveColumnName(parameter, matchingProperty),
                    FullyQualifiedReadType = GetReadTypeName(parameter.Type),
                    IsNullable = IsNullable(parameter.Type, parameter.NullableAnnotation),
                });
            }
            columns = ctorColumns;
            return ConstructionStrategy.Constructor;
        }

        var parameterlessCtor = typeSymbol.InstanceConstructors
            .FirstOrDefault(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);

        if (parameterlessCtor is not null && settableProperties.Count > 0)
        {
            var propColumns = new List<RowColumn>(settableProperties.Count);
            foreach (var property in settableProperties)
            {
                propColumns.Add(new RowColumn
                {
                    MemberName = property.Name,
                    ColumnName = ResolveColumnName(property),
                    FullyQualifiedReadType = GetReadTypeName(property.Type),
                    IsNullable = IsNullable(property.Type, property.NullableAnnotation),
                });
            }
            columns = propColumns;
            return ConstructionStrategy.ParameterlessThenSetters;
        }

        columns = [];
        return null;
    }

    private static IMethodSymbol? SelectPrimaryConstructor(INamedTypeSymbol typeSymbol)
    {
        var publicCtors = typeSymbol.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length > 0)
            .Where(c => !IsCopyConstructor(typeSymbol, c))
            .ToList();

        if (publicCtors.Count == 1)
            return publicCtors[0];

        return null;
    }

    private static bool IsCopyConstructor(INamedTypeSymbol typeSymbol, IMethodSymbol ctor)
    {
        if (!typeSymbol.IsRecord || ctor.Parameters.Length != 1)
            return false;
        return SymbolEqualityComparer.Default.Equals(ctor.Parameters[0].Type, typeSymbol);
    }

    private static IPropertySymbol? FindPropertyForParameter(INamedTypeSymbol typeSymbol, IParameterSymbol parameter)
    {
        var expectedPropertyName = parameter.Name.Length == 0
            ? parameter.Name
            : char.ToUpperInvariant(parameter.Name[0]) + parameter.Name.Substring(1);

        return typeSymbol.GetMembers(expectedPropertyName).OfType<IPropertySymbol>().FirstOrDefault()
            ?? typeSymbol.GetMembers(parameter.Name).OfType<IPropertySymbol>().FirstOrDefault();
    }

    private static string ResolveColumnName(IParameterSymbol parameter, IPropertySymbol? matchingProperty)
    {
        var fromParameter = ReadColumnAttributeName(parameter.GetAttributes());
        if (fromParameter is not null)
            return fromParameter;

        if (matchingProperty is not null)
        {
            var fromProperty = ReadColumnAttributeName(matchingProperty.GetAttributes());
            if (fromProperty is not null)
                return fromProperty;
            return matchingProperty.Name;
        }

        return parameter.Name;
    }

    private static string ResolveColumnName(IPropertySymbol property)
    {
        return ReadColumnAttributeName(property.GetAttributes()) ?? property.Name;
    }

    private static string? ReadColumnAttributeName(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass is null)
                continue;
            if (attribute.AttributeClass.ToDisplayString() != ColumnAttributeMetadataName)
                continue;
            if (attribute.ConstructorArguments.Length == 0)
                continue;
            var nameArgument = attribute.ConstructorArguments[0];
            if (nameArgument.Value is string name && name.Length > 0)
                return name;
        }
        return null;
    }

    private static string GetReadTypeName(ITypeSymbol type)
    {
        var underlying = type;
        if (type is INamedTypeSymbol named &&
            named.IsGenericType &&
            named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            underlying = named.TypeArguments[0];
        }
        return underlying.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static bool IsNullable(ITypeSymbol type, NullableAnnotation annotation)
    {
        if (type is INamedTypeSymbol named &&
            named.IsGenericType &&
            named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }
        if (type.IsValueType)
            return false;
        return annotation == NullableAnnotation.Annotated;
    }
}
