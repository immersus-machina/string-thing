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

        for (var i = 0; i < parameters.Count; i++)
        {
            parameters[i].ParameterName = TNamer.WritePlaceholder(i, parameterNames[i]);
            command.Parameters.Add(parameters[i]);
        }
    }
}
