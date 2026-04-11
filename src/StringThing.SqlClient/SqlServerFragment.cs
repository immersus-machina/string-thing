using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using StringThing.Core;

namespace StringThing.SqlClient;

/// <summary>
/// A composable SQL fragment that captures SQL Server parameters for splicing into a <see cref="SqlServerSql"/> statement.
/// </summary>
[InterpolatedStringHandler]
public sealed class SqlServerFragment : SqlFragment<SqlParameter>
{
    public SqlServerFragment(int literalLength, int formattedCount)
        : base(literalLength, formattedCount) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(SqlServerValue value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(value.ToSqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(SqlServerValue? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(
            value.HasValue ? value.Value.ToSqlParameter() : new SqlParameter { Value = DBNull.Value },
            expression);
}
