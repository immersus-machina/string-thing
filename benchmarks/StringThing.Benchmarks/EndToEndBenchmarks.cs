using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.Sqlite;
using StringThing.Sqlite;
using StringThing.Sqlite.Dapper;

namespace StringThing.Benchmarks;

[MemoryDiagnoser]
public class EndToEndBenchmarks
{
    private SqliteConnection _connection = null!;

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var create = _connection.CreateCommand();
        create.CommandText = """
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT,
                age INTEGER NOT NULL,
                active INTEGER NOT NULL
            )
            """;
        create.ExecuteNonQuery();

        for (var i = 1; i <= 100; i++)
        {
            using var insert = _connection.CreateCommand();
            insert.CommandText = $"INSERT INTO users (id, name, email, age, active) VALUES ({i}, 'user_{i}', 'user_{i}@example.com', {20 + i % 50}, {(i % 3 == 0 ? 0 : 1)})";
            insert.ExecuteNonQuery();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection.Dispose();
    }

    public record User(long Id, string Name, string? Email, long Age, long Active);

    // --- Single row query: 1 parameter ---

    [Benchmark(Description = "Raw: QuerySingle 1 param")]
    public User Raw_QuerySingle_OneParam()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, active FROM users WHERE id = @userId";
        cmd.Parameters.AddWithValue("@userId", 42);
        using var reader = cmd.ExecuteReader();
        reader.Read();
        return new User(reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4));
    }

    [Benchmark(Description = "Dapper: QuerySingle 1 param")]
    public User Dapper_QuerySingle_OneParam()
    {
        return _connection.QuerySingle<User>(
            "SELECT id, name, email, age, active FROM users WHERE id = @userId",
            new { userId = 42 });
    }

    [Benchmark(Description = "StringThing: QueryStringSingle 1 param")]
    public User StringThing_QuerySingle_OneParam()
    {
        var userId = 42;
        return _connection.QueryStringSingle<User>(
            $"SELECT id, name, email, age, active FROM users WHERE id = {userId}");
    }

    // --- Query: 2 parameters ---

    [Benchmark(Description = "Raw: Query 2 params")]
    public User Raw_Query_TwoParams()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, active FROM users WHERE age > @minAge AND active = @active ORDER BY id LIMIT 1";
        cmd.Parameters.AddWithValue("@minAge", 30);
        cmd.Parameters.AddWithValue("@active", 1);
        using var reader = cmd.ExecuteReader();
        reader.Read();
        return new User(reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4));
    }

    [Benchmark(Description = "Dapper: Query 2 params")]
    public User Dapper_Query_TwoParams()
    {
        return _connection.QuerySingle<User>(
            "SELECT id, name, email, age, active FROM users WHERE age > @minAge AND active = @active ORDER BY id LIMIT 1",
            new { minAge = 30, active = 1 });
    }

    [Benchmark(Description = "StringThing: QueryString 2 params")]
    public User StringThing_Query_TwoParams()
    {
        var minAge = 30;
        var active = 1;
        return _connection.QueryStringSingle<User>(
            $"SELECT id, name, email, age, active FROM users WHERE age > {minAge} AND active = {active} ORDER BY id LIMIT 1");
    }

    // --- Query: 5 parameters ---

    [Benchmark(Description = "Raw: Query 5 params")]
    public User Raw_Query_FiveParams()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, active FROM users WHERE id > @minId AND id < @maxId AND age >= @minAge AND age <= @maxAge AND active = @active LIMIT 1";
        cmd.Parameters.AddWithValue("@minId", 10);
        cmd.Parameters.AddWithValue("@maxId", 50);
        cmd.Parameters.AddWithValue("@minAge", 25);
        cmd.Parameters.AddWithValue("@maxAge", 40);
        cmd.Parameters.AddWithValue("@active", 1);
        using var reader = cmd.ExecuteReader();
        reader.Read();
        return new User(reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4));
    }

    [Benchmark(Description = "Dapper: Query 5 params")]
    public User Dapper_Query_FiveParams()
    {
        return _connection.QuerySingle<User>(
            "SELECT id, name, email, age, active FROM users WHERE id > @minId AND id < @maxId AND age >= @minAge AND age <= @maxAge AND active = @active LIMIT 1",
            new { minId = 10, maxId = 50, minAge = 25, maxAge = 40, active = 1 });
    }

    [Benchmark(Description = "StringThing: QueryString 5 params")]
    public User StringThing_Query_FiveParams()
    {
        var minId = 10;
        var maxId = 50;
        var minAge = 25;
        var maxAge = 40;
        var active = 1;
        return _connection.QueryStringSingle<User>(
            $"SELECT id, name, email, age, active FROM users WHERE id > {minId} AND id < {maxId} AND age >= {minAge} AND age <= {maxAge} AND active = {active} LIMIT 1");
    }

    // --- Execute (insert) ---

    private int _insertCounter = 1000;

    [Benchmark(Description = "Raw: Execute insert")]
    public int Raw_Execute_Insert()
    {
        var id = Interlocked.Increment(ref _insertCounter);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO users (id, name, email, age, active) VALUES (@id, @name, @email, @age, @active)";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", "bench");
        cmd.Parameters.AddWithValue("@email", "bench@test.com");
        cmd.Parameters.AddWithValue("@age", 30);
        cmd.Parameters.AddWithValue("@active", 1);
        return cmd.ExecuteNonQuery();
    }

    [Benchmark(Description = "Dapper: Execute insert")]
    public int Dapper_Execute_Insert()
    {
        var id = Interlocked.Increment(ref _insertCounter);
        return _connection.Execute(
            "INSERT OR IGNORE INTO users (id, name, email, age, active) VALUES (@id, @name, @email, @age, @active)",
            new { id, name = "bench", email = "bench@test.com", age = 30, active = 1 });
    }

    [Benchmark(Description = "StringThing: ExecuteString insert")]
    public int StringThing_Execute_Insert()
    {
        var id = Interlocked.Increment(ref _insertCounter);
        var name = "bench";
        var email = "bench@test.com";
        var age = 30;
        var active = 1;
        return _connection.ExecuteString(
            $"INSERT OR IGNORE INTO users (id, name, email, age, active) VALUES ({id}, {name}, {email}, {age}, {active})");
    }
}
