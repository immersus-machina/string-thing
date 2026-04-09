using System.Data;
using System.Data.Common;
using Dapper;
using Npgsql;

namespace StringThing.Npgsql.Dapper;

public static class PostgresSqlDapperExtensions
{
    // --- Query<T> ---

    public static IEnumerable<T> Query<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().ToList();
    }

    public static async Task<IEnumerable<T>> QueryAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().ToList();
    }

    // --- QueryFirst<T> ---

    public static T QueryFirst<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().First();
    }

    public static async Task<T> QueryFirstAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().First();
    }

    // --- QueryFirstOrDefault<T> ---

    public static T? QueryFirstOrDefault<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().FirstOrDefault();
    }

    public static async Task<T?> QueryFirstOrDefaultAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().FirstOrDefault();
    }

    // --- QuerySingle<T> ---

    public static T QuerySingle<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().Single();
    }

    public static async Task<T> QuerySingleAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().Single();
    }

    // --- QuerySingleOrDefault<T> ---

    public static T? QuerySingleOrDefault<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().SingleOrDefault();
    }

    public static async Task<T?> QuerySingleOrDefaultAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().SingleOrDefault();
    }

    // --- Execute ---

    public static int Execute(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        return command.ExecuteNonQuery();
    }

    public static async Task<int> ExecuteAsync(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // --- ExecuteScalar ---

    public static object? ExecuteScalar(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        return command.ExecuteScalar();
    }

    public static T? ExecuteScalar<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        var result = command.ExecuteScalar();
        return result is DBNull or null ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    public static async Task<object?> ExecuteScalarAsync(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    public static async Task<T?> ExecuteScalarAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is DBNull or null ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    // --- ExecuteReader ---

    public static IDataReader ExecuteReader(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        var command = statement.ToCommand(connection);
        return command.ExecuteReader(CommandBehavior.CloseConnection);
    }

    public static async Task<DbDataReader> ExecuteReaderAsync(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        var command = statement.ToCommand(connection);
        return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken);
    }
}
