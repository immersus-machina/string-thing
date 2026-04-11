using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using StringThing.Core;

namespace StringThing.Sqlite;

[InterpolatedStringHandler]
public class SqliteStatement<TNamer> : SqlStatement<TNamer, SqliteParameter>
    where TNamer : IParameterNamer
{
    public SqliteStatement(int literalLength, int formattedCount)
        : base(literalLength, formattedCount) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(SqliteValue value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(value.ToSqliteParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(SqliteValue? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(
            value.HasValue ? value.Value.ToSqliteParameter() : new SqliteParameter { Value = DBNull.Value },
            expression);
}
