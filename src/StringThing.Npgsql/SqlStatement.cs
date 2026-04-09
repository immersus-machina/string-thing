using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace StringThing.Npgsql;

[InterpolatedStringHandler]
public class SqlStatement<TNamer> where TNamer : IParameterNamer
{
    private const int MaxCharsPerPlaceholder = 32;

    private readonly StringBuilder _sql;
    private readonly List<NpgsqlParameter> _parameters;
    private readonly List<string?> _parameterNames;

    public SqlStatement(int literalLength, int formattedCount)
    {
        _sql = new StringBuilder(literalLength + (formattedCount * MaxCharsPerPlaceholder));
        _parameters = new List<NpgsqlParameter>(formattedCount);
        _parameterNames = new List<string?>(formattedCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string literalText)
    {
        _sql.Append(literalText);
    }

    // --- Null helpers ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static NpgsqlParameter NullableParam<T>(T? value) where T : struct
        => value.HasValue ? new NpgsqlParameter<T>("", value.Value) : new NpgsqlParameter { Value = DBNull.Value };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static NpgsqlParameter NullableRefParam<T>(T? value) where T : class
        => value is not null ? new NpgsqlParameter<T>("", value) : new NpgsqlParameter { Value = DBNull.Value };

    // --- Numeric ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(bool value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<bool>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(bool? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(short value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<short>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(short? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(int value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<int>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(int? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(long value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<long>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(long? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(float value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<float>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(float? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(double value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<double>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(double? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(decimal value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<decimal>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(decimal? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(BigInteger value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<BigInteger>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(BigInteger? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    // --- Text ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(char value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<char>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(char? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(string? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableRefParam(value), expression);

    // --- UUID ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Guid value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<Guid>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Guid? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    // --- Date / Time ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateTime value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<DateTime>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateTime? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateTimeOffset value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<DateTimeOffset>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateTimeOffset? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateOnly value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<DateOnly>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateOnly? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(TimeOnly value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<TimeOnly>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(TimeOnly? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(TimeSpan value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<TimeSpan>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(TimeSpan? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlInterval value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlInterval>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlInterval? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    // --- Binary ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(byte[]? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableRefParam(value), expression);

    // --- Bit strings ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(BitArray? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableRefParam(value), expression);

    // --- Network ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IPAddress? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableRefParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(PhysicalAddress? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableRefParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlInet value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlInet>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlInet? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlCidr value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlCidr>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlCidr? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    // --- Geometric ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPoint value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlPoint>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPoint? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlBox value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlBox>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlBox? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlLSeg value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlLSeg>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlLSeg? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPath value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlPath>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPath? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlCircle value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlCircle>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlCircle? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPolygon value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlPolygon>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPolygon? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlLine value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlLine>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlLine? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    // --- Full-text search ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlTsVector? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableRefParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlTsQuery? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableRefParam(value), expression);

    // --- Ranges ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<int> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlRange<int>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<int>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<long> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlRange<long>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<long>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<decimal> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlRange<decimal>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<decimal>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateTime> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlRange<DateTime>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateTime>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateTimeOffset> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlRange<DateTimeOffset>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateTimeOffset>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateOnly> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(new NpgsqlParameter<NpgsqlRange<DateOnly>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateOnly>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendParameter(NullableParam(value), expression);

    // --- Arrays ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendArrayParameter<T>(IReadOnlyList<T> value, string? expression)
    {
        var array = value as T[] ?? [.. value];
        AppendParameter(new NpgsqlParameter<T[]>("", array), expression);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<bool> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<short> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<int> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<long> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<float> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<double> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<decimal> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<string> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<Guid> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<DateTime> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<DateTimeOffset> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<DateOnly> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<TimeOnly> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<TimeSpan> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<IPAddress> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => AppendArrayParameter(value, expression);

    // --- UnsafeSql and Fragment splicing ---

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

    // --- Internal machinery ---

    private void AppendParameter(NpgsqlParameter parameter, string? expression, bool allowDeduplication = true)
    {
        var slotIndex = FindOrAddSlot(parameter, expression, allowDeduplication);

        Span<char> placeholderBuffer = stackalloc char[MaxCharsPerPlaceholder];
        TNamer.WritePlaceholder(
            slotIndex,
            (expression ?? string.Empty).AsSpan(),
            placeholderBuffer,
            MaxCharsPerPlaceholder,
            out var charactersWritten);
        _sql.Append(placeholderBuffer[..charactersWritten]);
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
