using Dapper;
using MySqlConnector;

namespace StringThing.MySql.Dapper;

public static class MySqlDapperExtensions
{
    // --- Query<T> ---

    public static List<T> QueryString<T>(
        this MySqlConnection connection,
        MySql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().ToList();
    }

    public static async Task<List<T>> QueryStringAsync<T>(
        this MySqlConnection connection,
        MySql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().ToList();
    }

    // --- QueryFirst<T> ---

    public static T QueryStringFirst<T>(
        this MySqlConnection connection,
        MySql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().First();
    }

    public static async Task<T> QueryStringFirstAsync<T>(
        this MySqlConnection connection,
        MySql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().First();
    }

    // --- QueryFirstOrDefault<T> ---

    public static T? QueryStringFirstOrDefault<T>(
        this MySqlConnection connection,
        MySql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().FirstOrDefault();
    }

    public static async Task<T?> QueryStringFirstOrDefaultAsync<T>(
        this MySqlConnection connection,
        MySql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().FirstOrDefault();
    }

    // --- QuerySingle<T> ---

    public static T QueryStringSingle<T>(
        this MySqlConnection connection,
        MySql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().Single();
    }

    public static async Task<T> QueryStringSingleAsync<T>(
        this MySqlConnection connection,
        MySql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().Single();
    }

    // --- QuerySingleOrDefault<T> ---

    public static T? QueryStringSingleOrDefault<T>(
        this MySqlConnection connection,
        MySql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().SingleOrDefault();
    }

    public static async Task<T?> QueryStringSingleOrDefaultAsync<T>(
        this MySqlConnection connection,
        MySql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().SingleOrDefault();
    }

    // --- Execute ---

    public static int ExecuteString(
        this MySqlConnection connection,
        MySql statement)
    {
        using var command = statement.ToCommand(connection);
        return command.ExecuteNonQuery();
    }

    public static async Task<int> ExecuteStringAsync(
        this MySqlConnection connection,
        MySql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // --- ExecuteScalar ---

    public static object? ExecuteStringScalar(
        this MySqlConnection connection,
        MySql statement)
    {
        using var command = statement.ToCommand(connection);
        return command.ExecuteScalar();
    }

    public static T? ExecuteStringScalar<T>(
        this MySqlConnection connection,
        MySql statement)
    {
        using var command = statement.ToCommand(connection);
        var result = command.ExecuteScalar();
        return result is DBNull or null ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    public static async Task<object?> ExecuteStringScalarAsync(
        this MySqlConnection connection,
        MySql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    public static async Task<T?> ExecuteStringScalarAsync<T>(
        this MySqlConnection connection,
        MySql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is DBNull or null ? default : (T)Convert.ChangeType(result, typeof(T));
    }
}
