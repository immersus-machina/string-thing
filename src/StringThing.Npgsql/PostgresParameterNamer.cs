using System.Runtime.CompilerServices;

namespace StringThing.Npgsql;

public sealed class PostgresParameterNamer : IParameterNamer
{
    public static void WritePlaceholder(
        int parameterIndex,
        ReadOnlySpan<char> capturedExpression,
        Span<char> destination,
        int maxCharsToWrite,
        out int charactersWritten)
    {
        destination[0] = '$';
        if (!(parameterIndex + 1).TryFormat(destination[1..maxCharsToWrite], out var digitsWritten))
            ThrowBudgetExceeded(parameterIndex, maxCharsToWrite);
        charactersWritten = 1 + digitsWritten;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowBudgetExceeded(int parameterIndex, int maxCharsToWrite) =>
        throw new InvalidOperationException(
            $"PostgresParameterNamer exceeded its {maxCharsToWrite}-char budget writing placeholder for parameter {parameterIndex + 1}.");
}
