namespace StringThing.Npgsql.Tests;

public sealed class NameCapturingParameterNamer : IParameterNamer
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

        if (capturedExpression.Length > 0)
        {
            // Replace dots with underscores for readability
            for (var i = 0; i < capturedExpression.Length && written < maxCharsToWrite; i++)
            {
                destination[written++] = capturedExpression[i] == '.' ? '_' : capturedExpression[i];
            }
        }
        else
        {
            var fallback = $"p{parameterIndex}";
            fallback.AsSpan().CopyTo(destination[written..]);
            written += fallback.Length;
        }

        charactersWritten = written;
    }
}
