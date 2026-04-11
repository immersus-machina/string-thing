using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;

namespace StringThing.SqlClient.Dapper;

public static class SqlServerSqlDapperExtensions
{
    // --- Query<T> ---

    public static IEnumerable<T> QueryString<T>(
        this SqlConnection connection,
        SqlServerSql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().ToList();
    }

    public static async Task<IEnumerable<T>> QueryStringAsync<T>(
        this SqlConnection connection,
        SqlServerSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().ToList();
    }

    // --- QueryFirst<T> ---

    public static T QueryStringFirst<T>(
        this SqlConnection connection,
        SqlServerSql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().First();
    }

    public static async Task<T> QueryStringFirstAsync<T>(
        this SqlConnection connection,
        SqlServerSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().First();
    }

    // --- QueryFirstOrDefault<T> ---

    public static T? QueryStringFirstOrDefault<T>(
        this SqlConnection connection,
        SqlServerSql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().FirstOrDefault();
    }

    public static async Task<T?> QueryStringFirstOrDefaultAsync<T>(
        this SqlConnection connection,
        SqlServerSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().FirstOrDefault();
    }

    // --- QuerySingle<T> ---

    public static T QueryStringSingle<T>(
        this SqlConnection connection,
        SqlServerSql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().Single();
    }

    public static async Task<T> QueryStringSingleAsync<T>(
        this SqlConnection connection,
        SqlServerSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().Single();
    }

    // --- QuerySingleOrDefault<T> ---

    public static T? QueryStringSingleOrDefault<T>(
        this SqlConnection connection,
        SqlServerSql statement)
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return reader.Parse<T>().SingleOrDefault();
    }

    public static async Task<T?> QueryStringSingleOrDefaultAsync<T>(
        this SqlConnection connection,
        SqlServerSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.Parse<T>().SingleOrDefault();
    }

    // --- Execute ---

    public static int ExecuteString(
        this SqlConnection connection,
        SqlServerSql statement)
    {
        using var command = statement.ToCommand(connection);
        return command.ExecuteNonQuery();
    }

    public static async Task<int> ExecuteStringAsync(
        this SqlConnection connection,
        SqlServerSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // --- ExecuteScalar ---

    public static object? ExecuteStringScalar(
        this SqlConnection connection,
        SqlServerSql statement)
    {
        using var command = statement.ToCommand(connection);
        return command.ExecuteScalar();
    }

    public static T? ExecuteStringScalar<T>(
        this SqlConnection connection,
        SqlServerSql statement)
    {
        using var command = statement.ToCommand(connection);
        var result = command.ExecuteScalar();
        return result is DBNull or null ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    public static async Task<object?> ExecuteStringScalarAsync(
        this SqlConnection connection,
        SqlServerSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    public static async Task<T?> ExecuteStringScalarAsync<T>(
        this SqlConnection connection,
        SqlServerSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is DBNull or null ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    // --- ExecuteReader ---

    public static IDataReader ExecuteStringReader(
        this SqlConnection connection,
        SqlServerSql statement)
    {
        var command = statement.ToCommand(connection);
        return command.ExecuteReader(CommandBehavior.CloseConnection);
    }

    public static async Task<DbDataReader> ExecuteStringReaderAsync(
        this SqlConnection connection,
        SqlServerSql statement,
        CancellationToken cancellationToken = default)
    {
        var command = statement.ToCommand(connection);
        return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellationToken);
    }
}
