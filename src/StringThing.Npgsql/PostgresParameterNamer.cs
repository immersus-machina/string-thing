namespace StringThing.Npgsql;

public sealed class PostgresParameterNamer : IParameterNamer
{
    public static string WritePlaceholder(
        int parameterIndex,
        string? capturedExpression)
    {
        return $"${parameterIndex + 1}";
    }
}
