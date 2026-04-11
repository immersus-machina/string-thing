using Microsoft.Data.Sqlite;
using StringThing.Core;

namespace StringThing.Sqlite;

public static class SqliteStatementExtensions
{
    public static SqliteCommand ToCommand(
        this SqliteStatement<NamedParameterNamer> statement,
        SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = statement.Sql;
        AddParameters(command, statement);
        return command;
    }

    public static SqliteCommand ToCommand(
        this SqliteStatement<IndexedParameterNamer> statement,
        SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = statement.Sql;
        AddParameters(command, statement);
        return command;
    }

    private static void AddParameters<TNamer>(SqliteCommand command, SqliteStatement<TNamer> statement)
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
