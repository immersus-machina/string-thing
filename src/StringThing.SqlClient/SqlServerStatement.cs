using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using StringThing.Core;

namespace StringThing.SqlClient;

[InterpolatedStringHandler]
public class SqlServerStatement<TNamer> : SqlStatement<TNamer, SqlParameter>
    where TNamer : IParameterNamer
{
    public SqlServerStatement(
        int literalLength,
        int formattedCount,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        : base(literalLength, formattedCount, filePath, lineNumber) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(SqlServerValue value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(value.ToSqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(SqlServerValue? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(
            value.HasValue ? value.Value.ToSqlParameter() : new SqlParameter { Value = DBNull.Value },
            expression);
}
