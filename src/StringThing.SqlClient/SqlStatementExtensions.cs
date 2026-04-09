using Microsoft.Data.SqlClient;

namespace StringThing.SqlClient;

public static class SqlStatementExtensions
{
    public static SqlCommand ToCommand(
        this SqlStatement<NamedParameterNamer> statement,
        SqlConnection connection)
    {
        var command = new SqlCommand(statement.Sql, connection);
        AddParameters(command, statement);
        return command;
    }

    public static SqlCommand ToCommand(
        this SqlStatement<IndexedParameterNamer> statement,
        SqlConnection connection)
    {
        var command = new SqlCommand(statement.Sql, connection);
        AddParameters(command, statement);
        return command;
    }

    private static void AddParameters<TNamer>(SqlCommand command, SqlStatement<TNamer> statement)
        where TNamer : IParameterNamer
    {
        var parameters = statement.Parameters;
        var parameterNames = statement.ParameterNames;
        Span<char> nameBuffer = stackalloc char[32];

        for (var i = 0; i < parameters.Count; i++)
        {
            TNamer.WritePlaceholder(
                i,
                (parameterNames[i] ?? string.Empty).AsSpan(),
                nameBuffer,
                32,
                out var nameLength);
            parameters[i].ParameterName = nameBuffer[..nameLength].ToString();
            command.Parameters.Add(parameters[i]);
        }
    }
}
