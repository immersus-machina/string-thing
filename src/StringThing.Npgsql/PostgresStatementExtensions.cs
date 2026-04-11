using Npgsql;

namespace StringThing.Npgsql;

public static class PostgresStatementExtensions
{
    public static NpgsqlCommand ToCommand(
        this PostgresStatement<PostgresParameterNamer> statement,
        NpgsqlConnection connection)
    {
        var command = new NpgsqlCommand(statement.Sql, connection);

        var parameters = statement.Parameters;
        for (var i = 0; i < parameters.Count; i++)
            command.Parameters.Add(parameters[i]);

        return command;
    }

    public static NpgsqlCommand ToCommand(
        this PostgresStatement<PostgresParameterNamer> statement,
        NpgsqlDataSource dataSource)
    {
        var command = dataSource.CreateCommand(statement.Sql);

        var parameters = statement.Parameters;
        for (var i = 0; i < parameters.Count; i++)
            command.Parameters.Add(parameters[i]);

        return command;
    }
}
