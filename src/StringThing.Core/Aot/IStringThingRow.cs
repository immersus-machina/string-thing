using System.Data.Common;

namespace StringThing.Aot;

/// <summary>
/// Implemented by types that can be materialized from a <see cref="DbDataReader"/> row.
/// Either implement manually, or annotate the type with <see cref="StringThingRowAttribute"/>
/// to have the implementation generated.
/// </summary>
public interface IStringThingRow<TSelf>
    where TSelf : IStringThingRow<TSelf>
{
    /// <summary>
    /// Column names in the binding order that <see cref="Read"/> expects.
    /// The <paramref name="ordinals"/> span passed to <see cref="Read"/> is parallel to this list:
    /// <c>ordinals[i]</c> is the resolved reader ordinal for <c>ColumnBindingOrder[i]</c>.
    /// </summary>
    static abstract ReadOnlySpan<string> ColumnBindingOrder { get; }

    /// <summary>
    /// Materializes a single row from <paramref name="reader"/> using pre-resolved column ordinals.
    /// </summary>
    static abstract TSelf Read(DbDataReader reader, ReadOnlySpan<int> ordinals);
}
