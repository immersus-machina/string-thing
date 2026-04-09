namespace StringThing;

public interface IParameterNamer
{
    static abstract void WritePlaceholder(
        int parameterIndex,
        ReadOnlySpan<char> capturedExpression,
        Span<char> destination,
        int maxCharsToWrite,
        out int charactersWritten);
}
