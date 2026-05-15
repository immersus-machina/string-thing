using System.Collections.Generic;

namespace StringThing.Aot.Generator;

internal sealed class RowModel
{
    public required string Namespace { get; init; }
    public required string TypeName { get; init; }
    public required string TypeKindKeyword { get; init; }
    public required bool IsRecord { get; init; }
    public required ConstructionStrategy Construction { get; init; }
    public required IReadOnlyList<RowColumn> Columns { get; init; }
    public required string HintName { get; init; }

    public string FullyQualifiedTypeName =>
        Namespace.Length == 0 ? TypeName : $"{Namespace}.{TypeName}";
}

internal enum ConstructionStrategy
{
    Constructor,
    ParameterlessThenSetters,
}

internal sealed class RowColumn
{
    public required string ColumnName { get; init; }
    public required string MemberName { get; init; }
    public required string FullyQualifiedReadType { get; init; }
    public required bool IsNullable { get; init; }
}
