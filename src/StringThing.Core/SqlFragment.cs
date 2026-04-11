using System.Runtime.CompilerServices;

namespace StringThing;

/// <summary>
/// A composable SQL fragment that captures parameters for splicing into a <see cref="SqlStatement{TNamer, TParameter}"/>.
/// </summary>
public abstract class SqlFragment<TParameter> where TParameter : class
{
    private readonly List<SqlElement<TParameter>> _elements;

    public SqlFragment(int literalLength, int formattedCount)
    {
        _elements = new List<SqlElement<TParameter>>((formattedCount * 2) + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string literalText)
    {
        _elements.Add(SqlElement<TParameter>.Literal(literalText));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(UnsafeSql rawSqlFragment)
    {
        _elements.Add(SqlElement<TParameter>.Literal(rawSqlFragment.RawText));
    }

    public void AppendFormatted(SqlFragment<TParameter> nestedFragment,
        [CallerArgumentExpression(nameof(nestedFragment))] string? expression = null)
    {
        var nestedElements = nestedFragment.Elements;
        var prefix = ResolveNamePrefix(expression);
        for (var i = 0; i < nestedElements.Count; i++)
        {
            var element = nestedElements[i];
            if (element.TryGetLiteral(out _))
            {
                _elements.Add(element);
            }
            else if (element.TryGetParameter(out var parameter, out var nestedExpression))
            {
                var combinedName = CombineNames(prefix, nestedExpression);
                _elements.Add(SqlElement<TParameter>.Param(parameter, combinedName));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RecordParameter(TParameter parameter, string? capturedExpression)
    {
        _elements.Add(SqlElement<TParameter>.Param(parameter, capturedExpression));
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

    internal IReadOnlyList<SqlElement<TParameter>> Elements => _elements;
}
