using System.Runtime.CompilerServices;
using Npgsql;
using StringThing.Core;

namespace StringThing.Npgsql;

[InterpolatedStringHandler]
public class PostgresStatement<TNamer> : SqlStatement<TNamer, NpgsqlParameter>
    where TNamer : IParameterNamer
{
    public PostgresStatement(int literalLength, int formattedCount)
        : base(literalLength, formattedCount) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(PostgresValue value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(value.ToNpgsqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(PostgresValue? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(
            value.HasValue ? value.Value.ToNpgsqlParameter() : new NpgsqlParameter { Value = DBNull.Value },
            expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IPostgresJson value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new PostgresValue(value).ToNpgsqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IPostgresJson[] values,
        [CallerArgumentExpression(nameof(values))] string? expression = null)
        => AppendParameter(new PostgresValue(values).ToNpgsqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(System.Text.Json.JsonDocument value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter { Value = value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb }, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(System.Text.Json.JsonElement value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter { Value = value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb }, expression);
}
