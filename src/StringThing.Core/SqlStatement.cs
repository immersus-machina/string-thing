using System.Runtime.CompilerServices;
using System.Text;

namespace StringThing;

public abstract class SqlStatement<TNamer, TParameter>
    where TNamer : IParameterNamer
    where TParameter : class
{
    private const int EstimatedCharsPerPlaceholder = 16;

    private readonly StringBuilder _sql;
    private readonly List<TParameter> _parameters;
    private readonly List<string?> _parameterNames;

    public SqlStatement(int literalLength, int formattedCount)
    {
        _sql = new StringBuilder(literalLength + (formattedCount * EstimatedCharsPerPlaceholder));
        _parameters = new List<TParameter>(formattedCount);
        _parameterNames = new List<string?>(formattedCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string literalText)
    {
        _sql.Append(literalText);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(UnsafeSql rawSqlFragment)
    {
        _sql.Append(rawSqlFragment.RawText);
    }

    public void AppendFormatted(SqlFragment<TParameter> fragment,
        [CallerArgumentExpression(nameof(fragment))] string? expression = null)
    {
        var prefix = SqlFragment<TParameter>.ResolveNamePrefix(expression);
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
                var combinedName = SqlFragment<TParameter>.CombineNames(prefix, nestedExpression);
                AppendParameter(parameter, combinedName, allowCrossContextDedup);
            }
        }
    }

    protected void AppendParameter(TParameter parameter, string? expression, bool allowDeduplication = true)
    {
        var slotIndex = FindOrAddSlot(parameter, expression, allowDeduplication);
        _sql.Append(TNamer.WritePlaceholder(slotIndex, expression));
    }

    private int FindOrAddSlot(TParameter parameter, string? expression, bool allowDeduplication)
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

    internal IReadOnlyList<TParameter> Parameters => _parameters;

    internal IReadOnlyList<string?> ParameterNames => _parameterNames;
}
