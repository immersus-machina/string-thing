namespace StringThing.Npgsql.Tests;

public sealed class NameCapturingParameterNamer : IParameterNamer
{
    public static string WritePlaceholder(
        int parameterIndex,
        string? capturedExpression)
    {
        if (capturedExpression is not null && capturedExpression.Length > 0)
            return $"@{capturedExpression.Replace('.', '_')}";

        return $"@p{parameterIndex}";
    }
}
