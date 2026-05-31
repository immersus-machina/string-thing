using System.Data.Common;

namespace StringThing.Aot;

/// <summary>
/// Materializes a single row from <paramref name="reader"/> using pre-resolved column ordinals.
/// </summary>
public delegate T StringThingRowMaterializer<T>(DbDataReader reader, ReadOnlySpan<int> ordinals);

/// <summary>
/// Holds the materializer and column binding order for a row type, registered at module load.
/// The unconstrained query methods read this instead of the <see cref="IStringThingRow{TSelf}"/>
/// static abstract members, which are only reachable through a generic constraint.
/// </summary>
public static class StringThingRowRegistry<T>
{
    private static StringThingRowMaterializer<T>? _read;
    private static string[]? _columnBindingOrder;

    public static bool IsRegistered => _read is not null;

    public static string[] ColumnBindingOrder =>
        _columnBindingOrder ?? throw NotRegistered();

    public static T Read(DbDataReader reader, ReadOnlySpan<int> ordinals) =>
        (_read ?? throw NotRegistered())(reader, ordinals);

    public static void Register(string[] columnBindingOrder, StringThingRowMaterializer<T> read)
    {
        _columnBindingOrder = columnBindingOrder;
        _read = read;
    }

    private static InvalidOperationException NotRegistered() =>
        new($"No StringThing row materializer is registered for '{typeof(T)}'.");
}

/// <summary>
/// Bridges a concrete row type's static abstract members into <see cref="StringThingRowRegistry{T}"/>.
/// The generated module initializer calls this with the concrete type, so the static abstract
/// members are reached through the constraint with no reflection.
/// </summary>
public static class RowRegistration
{
    public static void Register<T>()
        where T : IStringThingRow<T>
    {
        StringThingRowRegistry<T>.Register(
            T.ColumnBindingOrder.ToArray(),
            static (reader, ordinals) => T.Read(reader, ordinals));
    }
}
