using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using StringThing.Core;

namespace StringThing.Sqlite;

[InterpolatedStringHandler]
public sealed class SqliteFragment : SqlFragment<SqliteParameter>
{
    public SqliteFragment(int literalLength, int formattedCount)
        : base(literalLength, formattedCount) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(SqliteValue value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(value.ToSqliteParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(SqliteValue? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(
            value.HasValue ? value.Value.ToSqliteParameter() : new SqliteParameter { Value = DBNull.Value },
            expression);
}
