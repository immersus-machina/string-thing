using System.Text.RegularExpressions;

namespace StringThing.Core;

public sealed class NamedParameterNamer : IParameterNamer
{
    private static readonly Regex _indexedPlaceholderPattern = new(@"^p\d+$", RegexOptions.Compiled);
    private static readonly Regex _validParameterName = new(@"^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    public static string WritePlaceholder(
        int parameterIndex,
        string? capturedExpression)
    {
        if (capturedExpression is not null
            && capturedExpression.Length > 0
            && !capturedExpression.Contains('(')
            && !capturedExpression.Contains('"')
            && !capturedExpression.Contains('_'))
        {
            var name = capturedExpression
                .Replace(".", "_")
                .Replace("[", "__")
                .Replace("]", "");

            if (_validParameterName.IsMatch(name) && !_indexedPlaceholderPattern.IsMatch(name))
                return $"@{name}";
        }

        return $"@p{parameterIndex}";
    }
}
