using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace StringThing.SqlClient;

/// <summary>
/// A composable SQL fragment that captures parameters for splicing into a <see cref="SqlStatement{TNamer}"/>.
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RecordParameter(SqlParameter parameter, string? capturedExpression)
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
