using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using StringThing.Core;

namespace StringThing.Sqlite;

[InterpolatedStringHandler]
public sealed class SqliteSql : SqliteStatement<NamedParameterNamer>
{
    public SqliteSql(
        int literalLength,
        int formattedCount,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        : base(literalLength, formattedCount, filePath, lineNumber) { }

    /// <summary>
    /// Expands an array of values into a parenthesized, comma-separated list for use with IN clauses.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> is empty.</exception>
    public static SqlFragment<SqliteParameter> InList(
        SqliteValue[] values,
        [CallerArgumentExpression(nameof(values))] string? expression = null)
    {
        if (values.Length == 0)
            throw new ArgumentException("At least one value is required.", nameof(values));

        var elements = new List<SqlElement<SqliteParameter>>(values.Length * 2 + 1)
        {
            SqlElement<SqliteParameter>.Literal("(")
        };
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) elements.Add(SqlElement<SqliteParameter>.Literal(", "));
            elements.Add(SqlElement<SqliteParameter>.Param(values[i].ToSqliteParameter(), $"{expression}[{i}]"));
        }
        elements.Add(SqlElement<SqliteParameter>.Literal(")"));

        return SqlFragment<SqliteParameter>.CreateRecording(elements);
    }

    /// <summary>
    /// Composes multiple <see cref="ISqliteRow"/> instances into a comma-separated VALUES fragment
    /// for use in INSERT statements. Requires at least one row.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rows"/> is empty.</exception>
    public static SqlFragment<SqliteParameter> InsertRows<T>(IReadOnlyList<T> rows) where T : ISqliteRow
    {
        if (rows.Count == 0)
            throw new ArgumentException("At least one row is required.", nameof(rows));

        var elementsPerRow = rows[0].RowValues.Elements.Count;
        var elements = new List<SqlElement<SqliteParameter>>(rows.Count * elementsPerRow + rows.Count - 1);
        for (var i = 0; i < rows.Count; i++)
        {
            if (i > 0) elements.Add(SqlElement<SqliteParameter>.Literal(", "));
            elements.AddRange(rows[i].RowValues.Elements);
        }
        return SqlFragment<SqliteParameter>.CreateRecording(elements);
    }
}
