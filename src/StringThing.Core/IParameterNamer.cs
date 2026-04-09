namespace StringThing;

public interface IParameterNamer
{
    static abstract string WritePlaceholder(
        int parameterIndex,
        string? capturedExpression);
}
