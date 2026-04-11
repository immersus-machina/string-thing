namespace StringThing.Core;

public interface IParameterNamer
{
    static abstract string WritePlaceholder(
        int parameterIndex,
        string? capturedExpression);
}
