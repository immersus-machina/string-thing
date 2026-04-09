using System.Text.RegularExpressions;

namespace StringThing.SqlClient;

public sealed class NamedParameterNamer : IParameterNamer
{
    private static readonly Regex _indexedPlaceholderPattern = new(@"^p\d+$", RegexOptions.Compiled);

    public static string WritePlaceholder(
        int parameterIndex,
        string? capturedExpression)
    {
        if (capturedExpression is not null
            && capturedExpression.Length > 0
            && !capturedExpression.Contains('(')
            && char.IsLetter(capturedExpression[0]))
        {
            if (capturedExpression.Contains('_'))
                throw new InvalidOperationException(
                    $"Parameter expression '{capturedExpression}' contains an underscore, which conflicts with dot-to-underscore name mapping. Use a variable name without underscores.");

            if (_indexedPlaceholderPattern.IsMatch(capturedExpression))
                throw new InvalidOperationException(
                    $"Parameter expression '{capturedExpression}' matches the indexed placeholder pattern 'p{{digits}}', which conflicts with fallback parameter naming. Rename the variable.");

            return $"@{capturedExpression.Replace('.', '_')}";
        }

        return $"@p{parameterIndex}";
    }
}
