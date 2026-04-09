using System.Runtime.CompilerServices;
using System.Text;
using Npgsql;

namespace StringThing.Npgsql;

[InterpolatedStringHandler]
public class SqlStatement<TNamer> where TNamer : IParameterNamer
{
    private const int EstimatedCharsPerPlaceholder = 16;

    private readonly StringBuilder _sql;
    private readonly List<NpgsqlParameter> _parameters;
    private readonly List<string?> _parameterNames;

    public SqlStatement(int literalLength, int formattedCount)
    {
        _sql = new StringBuilder(literalLength + (formattedCount * EstimatedCharsPerPlaceholder));
        _parameters = new List<NpgsqlParameter>(formattedCount);
        _parameterNames = new List<string?>(formattedCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string literalText)
    {
        _sql.Append(literalText);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(PostgresValue value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(value.ToNpgsqlParameter(), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(PostgresValue? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(value.HasValue ? value.Value.ToNpgsqlParameter() : new NpgsqlParameter { Value = DBNull.Value }, expression);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(UnsafeSql rawSqlFragment)
    {
        _sql.Append(rawSqlFragment.RawText);
    }

    public void AppendFormatted(SqlFragment fragment,
        [CallerArgumentExpression(nameof(fragment))] string? expression = null)
    {
        var prefix = SqlFragment.ResolveNamePrefix(expression);
        var allowCrossContextDedup = prefix is not null;
        var elements = fragment.Elements;
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            if (element.TryGetLiteral(out var literalText))
            {
                _sql.Append(literalText);
            }
            else if (element.TryGetParameter(out var parameter, out var nestedExpression))
            {
                var combinedName = SqlFragment.CombineNames(prefix, nestedExpression);
                AppendParameter(parameter, combinedName, allowCrossContextDedup);
            }
        }
    }

    private void AppendParameter(NpgsqlParameter parameter, string? expression, bool allowDeduplication = true)
    {
        var slotIndex = FindOrAddSlot(parameter, expression, allowDeduplication);
        _sql.Append(TNamer.WritePlaceholder(slotIndex, expression));
    }

    private int FindOrAddSlot(NpgsqlParameter parameter, string? expression, bool allowDeduplication)
    {
        if (allowDeduplication && expression is not null && !expression.Contains('('))
        {
            for (var existingIndex = 0; existingIndex < _parameterNames.Count; existingIndex++)
            {
                if (string.Equals(_parameterNames[existingIndex], expression, StringComparison.Ordinal))
                    return existingIndex;
            }
        }

        _parameters.Add(parameter);
        _parameterNames.Add(expression);
        return _parameters.Count - 1;
    }

    internal string Sql => _sql.ToString();

    internal IReadOnlyList<NpgsqlParameter> Parameters => _parameters;

    internal IReadOnlyList<string?> ParameterNames => _parameterNames;
}
