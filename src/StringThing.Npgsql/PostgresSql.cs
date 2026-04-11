using System.Runtime.CompilerServices;
using Npgsql;
using StringThing.Core;

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
    public static SqlFragment<NpgsqlParameter> InsertRows<T>(IReadOnlyList<T> rows) where T : IPostgresRow
    {
        if (rows.Count == 0)
            throw new ArgumentException("At least one row is required.", nameof(rows));

        var elementsPerRow = rows[0].RowValues.Elements.Count;
        var elements = new List<SqlElement<NpgsqlParameter>>(rows.Count * elementsPerRow + rows.Count - 1);
        for (var i = 0; i < rows.Count; i++)
        {
            if (i > 0) elements.Add(SqlElement<NpgsqlParameter>.Literal(", "));
            elements.AddRange(rows[i].RowValues.Elements);
        }
        return SqlFragment<NpgsqlParameter>.CreateRecording(elements);
    }
}
