using System.Runtime.CompilerServices;

namespace StringThing.SqlClient;

public sealed class IndexedParameterNamer : IParameterNamer
{
    public static void WritePlaceholder(
        int parameterIndex,
        ReadOnlySpan<char> capturedExpression,
        Span<char> destination,
        int maxCharsToWrite,
        out int charactersWritten)
    {
        destination[0] = '@';
        destination[1] = 'p';
        if (!parameterIndex.TryFormat(destination[2..maxCharsToWrite], out var digitsWritten))
            ThrowBudgetExceeded(parameterIndex, maxCharsToWrite);
        charactersWritten = 2 + digitsWritten;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowBudgetExceeded(int parameterIndex, int maxCharsToWrite) =>
        throw new InvalidOperationException(
            $"IndexedParameterNamer exceeded its {maxCharsToWrite}-char budget writing placeholder for parameter {parameterIndex}.");
}
