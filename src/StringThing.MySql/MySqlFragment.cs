using System.Runtime.CompilerServices;
using MySqlConnector;
using StringThing.Core;

namespace StringThing.MySql;

[InterpolatedStringHandler]
public sealed class MySqlFragment : SqlFragment<MySqlParameter>
{
    public MySqlFragment(int literalLength, int formattedCount)
        : base(literalLength, formattedCount) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(MySqlValue value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(value.ToMySqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(MySqlValue? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(
            value.HasValue ? value.Value.ToMySqlParameter() : new MySqlParameter { Value = DBNull.Value },
            expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IMySqlJson value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new MySqlParameter { Value = value.ToJson(), MySqlDbType = MySqlDbType.JSON }, expression);
}
