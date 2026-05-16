using System.Data.Common;
using Npgsql;
using StringThing.Aot;

namespace StringThing.Npgsql;

public static class PostgresResultExtensions
{
    public static List<T> QueryString<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
        where T : IStringThingRow<T>
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        return ReadAll<T>(reader, statement);
    }

    public static async Task<List<T>> QueryStringAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
        where T : IStringThingRow<T>
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadAllAsync<T>(reader, statement, cancellationToken);
    }

    public static T QueryStringFirst<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
        where T : IStringThingRow<T>
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        var ordinals = ResolveOrdinals<T>(reader, statement);
        if (!reader.Read())
            throw new InvalidOperationException("Sequence contains no elements.");
        return T.Read(reader, ordinals);
    }

    public static async Task<T> QueryStringFirstAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
        where T : IStringThingRow<T>
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ordinals = ResolveOrdinals<T>(reader, statement);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Sequence contains no elements.");
        return T.Read(reader, ordinals);
    }

    public static T? QueryStringFirstOrDefault<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
        where T : IStringThingRow<T>
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        var ordinals = ResolveOrdinals<T>(reader, statement);
        if (!reader.Read())
            return default;
        return T.Read(reader, ordinals);
    }

    public static async Task<T?> QueryStringFirstOrDefaultAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
        where T : IStringThingRow<T>
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ordinals = ResolveOrdinals<T>(reader, statement);
        if (!await reader.ReadAsync(cancellationToken))
            return default;
        return T.Read(reader, ordinals);
    }

    public static T QueryStringSingle<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
        where T : IStringThingRow<T>
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        var ordinals = ResolveOrdinals<T>(reader, statement);
        if (!reader.Read())
            throw new InvalidOperationException("Sequence contains no elements.");
        var first = T.Read(reader, ordinals);
        if (reader.Read())
            throw new InvalidOperationException("Sequence contains more than one element.");
        return first;
    }

    public static async Task<T> QueryStringSingleAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
        where T : IStringThingRow<T>
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ordinals = ResolveOrdinals<T>(reader, statement);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Sequence contains no elements.");
        var first = T.Read(reader, ordinals);
        if (await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Sequence contains more than one element.");
        return first;
    }

    public static T? QueryStringSingleOrDefault<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
        where T : IStringThingRow<T>
    {
        using var command = statement.ToCommand(connection);
        using var reader = command.ExecuteReader();
        var ordinals = ResolveOrdinals<T>(reader, statement);
        if (!reader.Read())
            return default;
        var first = T.Read(reader, ordinals);
        if (reader.Read())
            throw new InvalidOperationException("Sequence contains more than one element.");
        return first;
    }

    public static async Task<T?> QueryStringSingleOrDefaultAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
        where T : IStringThingRow<T>
    {
        await using var command = statement.ToCommand(connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ordinals = ResolveOrdinals<T>(reader, statement);
        if (!await reader.ReadAsync(cancellationToken))
            return default;
        var first = T.Read(reader, ordinals);
        if (await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Sequence contains more than one element.");
        return first;
    }

    public static int ExecuteString(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        return command.ExecuteNonQuery();
    }

    public static async Task<int> ExecuteStringAsync(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static object? ExecuteStringScalar(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        return command.ExecuteScalar();
    }

    public static T? ExecuteStringScalar<T>(
        this NpgsqlConnection connection,
        PostgresSql statement)
    {
        using var command = statement.ToCommand(connection);
        var result = command.ExecuteScalar();
        return result is DBNull or null ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    public static async Task<object?> ExecuteStringScalarAsync(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    public static async Task<T?> ExecuteStringScalarAsync<T>(
        this NpgsqlConnection connection,
        PostgresSql statement,
        CancellationToken cancellationToken = default)
    {
        await using var command = statement.ToCommand(connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is DBNull or null ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    private static int[] ResolveOrdinals<T>(DbDataReader reader, PostgresSql statement)
        where T : IStringThingRow<T>
    {
        if (statement.TryGetCachedOrdinals(typeof(T), out var cached))
            return cached;

        var names = T.ColumnBindingOrder;
        var ordinals = new int[names.Length];
        for (var i = 0; i < names.Length; i++)
            ordinals[i] = reader.GetOrdinal(names[i]);
        statement.CacheOrdinals(typeof(T), ordinals);
        return ordinals;
    }

    private static List<T> ReadAll<T>(DbDataReader reader, PostgresSql statement)
        where T : IStringThingRow<T>
    {
        var ordinals = ResolveOrdinals<T>(reader, statement);
        var list = new List<T>();
        while (reader.Read())
            list.Add(T.Read(reader, ordinals));
        return list;
    }

    private static async Task<List<T>> ReadAllAsync<T>(DbDataReader reader, PostgresSql statement, CancellationToken cancellationToken)
        where T : IStringThingRow<T>
    {
        var ordinals = ResolveOrdinals<T>(reader, statement);
        var list = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
            list.Add(T.Read(reader, ordinals));
        return list;
    }
}
