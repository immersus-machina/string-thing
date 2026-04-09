using System.Data;
using System.Runtime.CompilerServices;

namespace StringThing.SqlClient;

[InterpolatedStringHandler]
public sealed class SqlServerSql : SqlStatement<NamedParameterNamer>
{
    public SqlServerSql(int literalLength, int formattedCount)
        : base(literalLength, formattedCount) { }

    /// <summary>
    /// Creates a VarChar (non-Unicode) parameter instead of the default NVarChar.
    /// </summary>
    public static SqlServerValue VarChar(string value) => new(value, SqlDbType.VarChar);

    /// <summary>
    /// Creates a legacy DateTime parameter instead of the default DateTime2.
    /// </summary>
    public static SqlServerValue DateTime(DateTime value) => new(value, SqlDbType.DateTime);

    /// <summary>
    /// Composes multiple <see cref="ISqlServerRow"/> instances into a comma-separated VALUES fragment
    /// for use in INSERT statements. Requires at least one row.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rows"/> is empty.</exception>
    public static SqlFragment InsertRows<T>(IReadOnlyList<T> rows) where T : ISqlServerRow
    {
        if (rows.Count == 0)
            throw new ArgumentException("At least one row is required.", nameof(rows));

        SqlFragment result = rows[0].RowValues;
        for (var i = 1; i < rows.Count; i++)
        {
            var previous = result;
            result = $"{previous}, {rows[i].RowValues}";
        }
        return result;
    }
}
