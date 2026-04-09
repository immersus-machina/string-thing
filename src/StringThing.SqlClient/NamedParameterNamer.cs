using System.Runtime.CompilerServices;

namespace StringThing.SqlClient;

public sealed class NamedParameterNamer : IParameterNamer
{
    public static void WritePlaceholder(
        int parameterIndex,
        ReadOnlySpan<char> capturedExpression,
        Span<char> destination,
        int maxCharsToWrite,
        out int charactersWritten)
    {
        destination[0] = '@';
        var written = 1;

        if (capturedExpression.Length > 0
            && !capturedExpression.Contains('(')
            && char.IsLetter(capturedExpression[0]))
        {
            if (CollidesWithIndexedPlaceholder(capturedExpression))
                ThrowReservedName(capturedExpression);

            foreach (var character in capturedExpression)
            {
                if (character == '_')
                    ThrowUnderscoreNotAllowed(capturedExpression);
                destination[written++] = character == '.' ? '_' : character;
            }
        }
        else
        {
            destination[1] = 'p';
            if (!parameterIndex.TryFormat(destination[2..maxCharsToWrite], out var digitsWritten))
                ThrowBudgetExceeded(parameterIndex, maxCharsToWrite);
            written = 2 + digitsWritten;
        }

        charactersWritten = written;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnderscoreNotAllowed(ReadOnlySpan<char> expression) =>
        throw new InvalidOperationException(
            $"Parameter expression '{expression}' contains an underscore, which conflicts with dot-to-underscore name mapping. Use a variable name without underscores.");

    private static bool CollidesWithIndexedPlaceholder(ReadOnlySpan<char> expression)
    {
        if (expression.Length < 2 || expression[0] != 'p')
            return false;
        for (var i = 1; i < expression.Length; i++)
        {
            if (!char.IsDigit(expression[i]))
                return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowReservedName(ReadOnlySpan<char> expression) =>
        throw new InvalidOperationException(
            $"Parameter expression '{expression}' matches the indexed placeholder pattern 'p{{digits}}', which conflicts with fallback parameter naming. Rename the variable.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowBudgetExceeded(int parameterIndex, int maxCharsToWrite) =>
        throw new InvalidOperationException(
            $"NamedParameterNamer exceeded its {maxCharsToWrite}-char budget writing placeholder for parameter {parameterIndex}.");
}
