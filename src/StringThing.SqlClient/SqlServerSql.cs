using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

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
    public static SqlFragment<SqlParameter> InList(
        SqlServerValue[] values,
        [CallerArgumentExpression(nameof(values))] string? expression = null)
    {
        if (values.Length == 0)
            throw new ArgumentException("At least one value is required.", nameof(values));

        var elements = new List<SqlElement<SqlParameter>>(values.Length * 2 + 1)
        {
            SqlElement<SqlParameter>.Literal("(")
        };
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) elements.Add(SqlElement<SqlParameter>.Literal(", "));
            elements.Add(SqlElement<SqlParameter>.Param(values[i].ToSqlParameter(), $"{expression}[{i}]"));
        }
        elements.Add(SqlElement<SqlParameter>.Literal(")"));

        return SqlFragment<SqlParameter>.CreateRecording(elements);
    }

    /// <summary>
    /// Composes multiple <see cref="ISqlServerRow"/> instances into a comma-separated VALUES fragment
    /// for use in INSERT statements. Requires at least one row.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rows"/> is empty.</exception>
    public static SqlFragment<SqlParameter> InsertRows<T>(IReadOnlyList<T> rows) where T : ISqlServerRow
    {
        if (rows.Count == 0)
            throw new ArgumentException("At least one row is required.", nameof(rows));

        var elementsPerRow = rows[0].RowValues.Elements.Count;
        var elements = new List<SqlElement<SqlParameter>>(rows.Count * elementsPerRow + rows.Count - 1);
        for (var i = 0; i < rows.Count; i++)
        {
            if (i > 0) elements.Add(SqlElement<SqlParameter>.Literal(", "));
            elements.AddRange(rows[i].RowValues.Elements);
        }
        return SqlFragment<SqlParameter>.CreateRecording(elements);
    }
}
