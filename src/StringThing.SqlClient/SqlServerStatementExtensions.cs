using Microsoft.Data.SqlClient;
using StringThing.Core;

namespace StringThing.SqlClient;

public static class SqlServerStatementExtensions
{
    public static SqlCommand ToCommand(
        this SqlServerStatement<NamedParameterNamer> statement,
        SqlConnection connection)
    {
        var command = new SqlCommand(statement.Sql, connection);
        AddParameters(command, statement);
        return command;
    }

    public static SqlCommand ToCommand(
        this SqlServerStatement<IndexedParameterNamer> statement,
        SqlConnection connection)
    {
        var command = new SqlCommand(statement.Sql, connection);
        AddParameters(command, statement);
        return command;
    }

    private static void AddParameters<TNamer>(SqlCommand command, SqlServerStatement<TNamer> statement)
        where TNamer : IParameterNamer
    {
        var parameters = statement.Parameters;
        var cachedPlaceholders = statement.CachedPlaceholders;

        if (cachedPlaceholders is not null)
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                parameters[i].ParameterName = cachedPlaceholders[i];
                command.Parameters.Add(parameters[i]);
            }
        }
        else
        {
            var parameterNames = statement.ParameterNames;
            for (var i = 0; i < parameters.Count; i++)
            {
                parameters[i].ParameterName = TNamer.WritePlaceholder(i, parameterNames[i]);
                command.Parameters.Add(parameters[i]);
            }
        }
    }
}
