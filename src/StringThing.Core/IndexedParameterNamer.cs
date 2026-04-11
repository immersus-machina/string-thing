namespace StringThing.Core;

public sealed class IndexedParameterNamer : IParameterNamer
{
    public static string WritePlaceholder(
        int parameterIndex,
        string? capturedExpression)
    {
        return $"@p{parameterIndex}";
    }
}
