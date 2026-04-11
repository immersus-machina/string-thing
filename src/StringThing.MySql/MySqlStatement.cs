using System.Runtime.CompilerServices;
using MySqlConnector;
using StringThing.Core;

namespace StringThing.MySql;

[InterpolatedStringHandler]
public class MySqlStatement<TNamer> : SqlStatement<TNamer, MySqlParameter>
    where TNamer : IParameterNamer
{
    public MySqlStatement(int literalLength, int formattedCount)
        : base(literalLength, formattedCount) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(MySqlValue value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(value.ToMySqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(MySqlValue? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(
            value.HasValue ? value.Value.ToMySqlParameter() : new MySqlParameter { Value = DBNull.Value },
            expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IMySqlJson value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new MySqlParameter { Value = value.ToJson(), MySqlDbType = MySqlDbType.JSON }, expression);
}
