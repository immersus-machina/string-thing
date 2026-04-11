using MySqlConnector;
using StringThing.Core;

namespace StringThing.MySql;

public static class MySqlStatementExtensions
{
    public static MySqlCommand ToCommand(
        this MySqlStatement<NamedParameterNamer> statement,
        MySqlConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = statement.Sql;
        AddParameters(command, statement);
        return command;
    }

    public static MySqlCommand ToCommand(
        this MySqlStatement<IndexedParameterNamer> statement,
        MySqlConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = statement.Sql;
        AddParameters(command, statement);
        return command;
    }

    private static void AddParameters<TNamer>(MySqlCommand command, MySqlStatement<TNamer> statement)
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
