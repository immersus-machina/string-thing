using System.Data;
using System.Runtime.CompilerServices;

namespace StringThing.SqlClient;

[InterpolatedStringHandler]
public sealed class SqlServerSql : SqlServerStatement<NamedParameterNamer>
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
    /// Creates a Table-Valued Parameter for use in SQL Server queries.
    /// Requires a matching type defined on the server (CREATE TYPE ... AS TABLE).
    /// </summary>
    public static SqlServerValue Table(DataTable table, string typeName) => new(new SqlServerTable(table, typeName), SqlDbType.Structured);

    /// <summary>
    /// Expands an array of values into a parenthesized, comma-separated list for use with IN clauses.
    /// Example: <c>$"WHERE id IN {SqlServerSql.InList([1, 2, 3])}"</c> produces <c>WHERE id IN (@p0, @p1, @p2)</c>
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> is empty.</exception>
    public static SqlServerFragment InList(SqlServerValue[] values)
    {
        if (values.Length == 0)
            throw new ArgumentException("At least one value is required.", nameof(values));

        SqlServerFragment fragment = $"({values[0]}";
        for (var i = 1; i < values.Length; i++)
        {
            var previous = fragment;
            fragment = $"{previous}, {values[i]}";
        }
        var result = fragment;
        return $"{result})";
    }

    /// <summary>
    /// Composes multiple <see cref="ISqlServerRow"/> instances into a comma-separated VALUES fragment
    /// for use in INSERT statements. Requires at least one row.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rows"/> is empty.</exception>
    public static SqlServerFragment InsertRows<T>(IReadOnlyList<T> rows) where T : ISqlServerRow
    {
        if (rows.Count == 0)
            throw new ArgumentException("At least one row is required.", nameof(rows));

        return rows
            .Skip(1)
            .Aggregate(rows[0].RowValues, (result, row) => $"{result}, {row.RowValues}");
    }
}
