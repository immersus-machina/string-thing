using System.Data.Common;

namespace StringThing.Aot;

/// <summary>
/// Reads a single-column scalar result for a query whose <c>T</c> is a language primitive
/// rather than a <see cref="IStringThingRow{TSelf}"/> row type.
/// </summary>
public static class ScalarValue
{
    /// <summary>
    /// Reads column <paramref name="ordinal"/> as <typeparamref name="T"/>. A SQL <c>NULL</c> yields
    /// <c>default</c> when <typeparamref name="T"/> is nullable; for a non-nullable value type it
    /// throws via <see cref="DbDataReader.GetFieldValue{T}"/>, consistent with the row mapping path.
    /// </summary>
    public static T Read<T>(DbDataReader reader, int ordinal)
    {
        if (ScalarNullability<T>.IsNullable && reader.IsDBNull(ordinal))
            return default!;
        return reader.GetFieldValue<T>(ordinal);
    }

    private static class ScalarNullability<T>
    {
        public static readonly bool IsNullable =
            !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) is not null;
    }
}
