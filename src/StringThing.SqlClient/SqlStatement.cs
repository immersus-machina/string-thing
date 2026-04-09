using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Data.SqlClient;

namespace StringThing.SqlClient;

[InterpolatedStringHandler]
public class SqlStatement<TNamer> where TNamer : IParameterNamer
{
    private const int MaxCharsPerPlaceholder = 32;

    private readonly StringBuilder _sql;
    private readonly List<SqlParameter> _parameters;
    private readonly List<string?> _parameterNames;

    public SqlStatement(int literalLength, int formattedCount)
    {
        _sql = new StringBuilder(literalLength + (formattedCount * MaxCharsPerPlaceholder));
        _parameters = new List<SqlParameter>(formattedCount);
        _parameterNames = new List<string?>(formattedCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string literalText)
    {
        _sql.Append(literalText);
    }

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

    private void AppendParameter(SqlParameter parameter, string? expression, bool allowDeduplication = true)
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

    private int FindOrAddSlot(SqlParameter parameter, string? expression, bool allowDeduplication)
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

    internal IReadOnlyList<SqlParameter> Parameters => _parameters;

    internal IReadOnlyList<string?> ParameterNames => _parameterNames;
}
