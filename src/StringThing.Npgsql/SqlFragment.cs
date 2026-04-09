using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;
using Npgsql;
using NpgsqlTypes;

namespace StringThing.Npgsql;

/// <summary>
/// A composable SQL fragment that captures parameters for splicing into a <see cref="SqlStatement{TNamer}"/>.
/// The creator is responsible for disposing the fragment; splicing into a statement does not transfer ownership.
/// </summary>
[InterpolatedStringHandler]
public sealed class SqlFragment
{
    private readonly List<SqlElement> _elements;
    private int _totalLiteralChars;
    private int _parameterCount;

    public SqlFragment(int literalLength, int formattedCount)
    {
        _elements = new List<SqlElement>((formattedCount * 2) + 1);
        _totalLiteralChars = 0;
        _parameterCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string literalText)
    {
        _elements.Add(SqlElement.Literal(literalText));
        _totalLiteralChars += literalText.Length;
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
        => RecordParameter(new NpgsqlParameter<bool>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(bool? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(short value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<short>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(short? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(int value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<int>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(int? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(long value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<long>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(long? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(float value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<float>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(float? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(double value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<double>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(double? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(decimal value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<decimal>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(decimal? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(BigInteger value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<BigInteger>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(BigInteger? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    // --- Text ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(char value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<char>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(char? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(string? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableRefParam(value), expression);

    // --- UUID ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Guid value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<Guid>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Guid? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    // --- Date / Time ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateTime value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<DateTime>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateTime? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateTimeOffset value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<DateTimeOffset>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateTimeOffset? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateOnly value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<DateOnly>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(DateOnly? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(TimeOnly value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<TimeOnly>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(TimeOnly? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(TimeSpan value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<TimeSpan>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(TimeSpan? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlInterval value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlInterval>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlInterval? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    // --- Binary ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(byte[]? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableRefParam(value), expression);

    // --- Bit strings ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(BitArray? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableRefParam(value), expression);

    // --- Network ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IPAddress? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableRefParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(PhysicalAddress? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableRefParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlInet value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlInet>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlInet? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlCidr value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlCidr>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlCidr? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    // --- Geometric ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPoint value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlPoint>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPoint? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlBox value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlBox>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlBox? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlLSeg value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlLSeg>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlLSeg? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPath value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlPath>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPath? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlCircle value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlCircle>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlCircle? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPolygon value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlPolygon>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlPolygon? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlLine value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlLine>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlLine? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    // --- Full-text search ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlTsVector? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableRefParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlTsQuery? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableRefParam(value), expression);

    // --- Ranges ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<int> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlRange<int>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<int>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<long> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlRange<long>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<long>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<decimal> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlRange<decimal>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<decimal>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateTime> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlRange<DateTime>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateTime>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateTimeOffset> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlRange<DateTimeOffset>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateTimeOffset>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateOnly> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(new NpgsqlParameter<NpgsqlRange<DateOnly>>("", value), expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(NpgsqlRange<DateOnly>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordParameter(NullableParam(value), expression);

    // --- Arrays ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RecordArrayParameter<T>(IReadOnlyList<T> value, string? expression)
    {
        var array = value as T[] ?? [.. value];
        RecordParameter(new NpgsqlParameter<T[]>("", array), expression);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<bool> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<short> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<int> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<long> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<float> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<double> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<decimal> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<string> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<Guid> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<DateTime> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<DateTimeOffset> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<DateOnly> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<TimeOnly> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<TimeSpan> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IReadOnlyList<IPAddress> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        => RecordArrayParameter(value, expression);

    // --- UnsafeSql and nested Fragment splicing ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(UnsafeSql rawSqlFragment)
    {
        _elements.Add(SqlElement.Literal(rawSqlFragment.RawText));
        _totalLiteralChars += rawSqlFragment.RawText.Length;
    }

    public void AppendFormatted(SqlFragment nestedFragment,
        [CallerArgumentExpression(nameof(nestedFragment))] string? expression = null)
    {
        var nestedElements = nestedFragment.Elements;
        var prefix = ResolveNamePrefix(expression);
        for (var i = 0; i < nestedElements.Count; i++)
        {
            var element = nestedElements[i];
            if (element.TryGetLiteral(out var literalText))
            {
                _elements.Add(element);
                _totalLiteralChars += literalText.Length;
            }
            else if (element.TryGetParameter(out var parameter, out var nestedExpression))
            {
                var combinedName = CombineNames(prefix, nestedExpression);
                _elements.Add(SqlElement.Param(parameter, combinedName));
                _parameterCount++;
            }
        }
    }

    // --- Internal machinery ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RecordParameter(NpgsqlParameter parameter, string? capturedExpression)
    {
        _elements.Add(SqlElement.Param(parameter, capturedExpression));
        _parameterCount++;
    }

    internal static string? ResolveNamePrefix(string? capturedExpression)
    {
        if (capturedExpression is null ||
            capturedExpression.Contains('('))
        {
            return null;
        }
        return capturedExpression;
    }

    internal static string? CombineNames(string? prefix, string? innerName)
    {
        if (prefix is null || innerName is null)
            return null;
        return $"{prefix}.{innerName}";
    }

    internal IReadOnlyList<SqlElement> Elements => _elements;
    internal int TotalLiteralChars => _totalLiteralChars;
    internal int ParameterCount => _parameterCount;
}
