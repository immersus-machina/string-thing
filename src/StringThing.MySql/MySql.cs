using System.Runtime.CompilerServices;
using MySqlConnector;
using StringThing.Core;

namespace StringThing.MySql;

[InterpolatedStringHandler]
public sealed class MySql : MySqlStatement<NamedParameterNamer>
{
    public MySql(
        int literalLength,
        int formattedCount,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        : base(literalLength, formattedCount, filePath, lineNumber) { }

    /// <summary>
    /// Creates a TEXT parameter instead of the default VarChar.
    /// </summary>
    public static MySqlValue Text(string value) => new(value, MySqlDbType.Text);

    /// <summary>
    /// Creates a TIMESTAMP parameter instead of the default DateTime.
    /// </summary>
    public static MySqlValue Timestamp(DateTime value) => new(value, MySqlDbType.Timestamp);

    /// <summary>
    /// Expands an array of values into a parenthesized, comma-separated list for use with IN clauses.
    /// Example: <c>$"WHERE id IN {MySql.InList([1, 2, 3])}"</c>
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> is empty.</exception>
    public static SqlFragment<MySqlParameter> InList(
        MySqlValue[] values,
        [CallerArgumentExpression(nameof(values))] string? expression = null)
    {
        if (values.Length == 0)
            throw new ArgumentException("At least one value is required.", nameof(values));

        var elements = new List<SqlElement<MySqlParameter>>(values.Length * 2 + 1)
        {
            SqlElement<MySqlParameter>.Literal("(")
        };
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) elements.Add(SqlElement<MySqlParameter>.Literal(", "));
            elements.Add(SqlElement<MySqlParameter>.Param(values[i].ToMySqlParameter(), $"{expression}[{i}]"));
        }
        elements.Add(SqlElement<MySqlParameter>.Literal(")"));

        return SqlFragment<MySqlParameter>.CreateRecording(elements);
    }

    /// <summary>
    /// Composes multiple <see cref="IMySqlRow"/> instances into a comma-separated VALUES fragment
    /// for use in INSERT statements. Requires at least one row.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rows"/> is empty.</exception>
    public static SqlFragment<MySqlParameter> InsertRows<T>(IReadOnlyList<T> rows) where T : IMySqlRow
    {
        if (rows.Count == 0)
            throw new ArgumentException("At least one row is required.", nameof(rows));

        var elementsPerRow = rows[0].RowValues.Elements.Count;
        var elements = new List<SqlElement<MySqlParameter>>(rows.Count * elementsPerRow + rows.Count - 1);
        for (var i = 0; i < rows.Count; i++)
        {
            if (i > 0) elements.Add(SqlElement<MySqlParameter>.Literal(", "));
            elements.AddRange(rows[i].RowValues.Elements);
        }
        return SqlFragment<MySqlParameter>.CreateRecording(elements);
    }
}
