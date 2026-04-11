using System.Runtime.CompilerServices;

namespace StringThing.Npgsql;

[InterpolatedStringHandler]
public sealed class PostgresSql : PostgresStatement<PostgresParameterNamer>
{
    public PostgresSql(int literalLength, int formattedCount)
        : base(literalLength, formattedCount) { }

    /// <summary>
    /// Composes multiple <see cref="IPostgresRow"/> instances into a comma-separated VALUES fragment
    /// for use in INSERT statements. Requires at least one row.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rows"/> is empty.</exception>
    public static PostgresFragment InsertRows<T>(IReadOnlyList<T> rows) where T : IPostgresRow
    {
        if (rows.Count == 0)
            throw new ArgumentException("At least one row is required.", nameof(rows));

        return rows
            .Skip(1)
            .Aggregate(rows[0].RowValues, (result, row) => $"{result}, {row.RowValues}");
    }
}
