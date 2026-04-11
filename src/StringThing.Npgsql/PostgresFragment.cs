using System.Runtime.CompilerServices;
using Npgsql;
using StringThing.Core;

namespace StringThing.Npgsql;

/// <summary>
/// A composable SQL fragment that captures Postgres parameters for splicing into a <see cref="PostgresSql"/> statement.
/// </summary>
[InterpolatedStringHandler]
public sealed class PostgresFragment : SqlFragment<NpgsqlParameter>
{
    public PostgresFragment(int literalLength, int formattedCount)
        : base(literalLength, formattedCount) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(PostgresValue value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(value.ToNpgsqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(PostgresValue? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(
            value.HasValue ? value.Value.ToNpgsqlParameter() : new NpgsqlParameter { Value = DBNull.Value },
            expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IPostgresJson value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new PostgresValue(value).ToNpgsqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IPostgresJson[] values,
        [CallerArgumentExpression(nameof(values))] string? expression = null)
        => RecordParameter(new PostgresValue(values).ToNpgsqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(System.Text.Json.JsonDocument value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter { Value = value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb }, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(System.Text.Json.JsonElement value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter { Value = value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb }, expression);
}
