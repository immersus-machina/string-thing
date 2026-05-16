using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using StringThing.Aot;
using StringThing.Sqlite;

namespace StringThing.Benchmarks;

[MemoryDiagnoser]
public class RowMappingDiagnosticsBenchmarks
{
    private SqliteConnection _connection = null!;
    private const string OrdinalSql = "SELECT id, name, email, age, active FROM users WHERE id = @userId";

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

        using var insert = _connection.CreateCommand();
        insert.CommandText = "INSERT INTO users (id, name, email, age, active) VALUES (42, 'user_42', 'user_42@example.com', 27, 1)";
        insert.ExecuteNonQuery();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection.Dispose();
    }

    [Benchmark(Description = "A: Raw + ordinal indices + typed getters (baseline)")]
    public BenchmarkUser A_Raw_OrdinalIndex_TypedGetters()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = OrdinalSql;
        cmd.Parameters.AddWithValue("@userId", 42);
        using var reader = cmd.ExecuteReader();
        reader.Read();
        return new BenchmarkUser(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetInt64(3),
            reader.GetInt64(4));
    }

    [Benchmark(Description = "B: Raw + ordinal indices + GetFieldValue<T> (boxing check)")]
    public BenchmarkUser B_Raw_OrdinalIndex_GetFieldValue()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = OrdinalSql;
        cmd.Parameters.AddWithValue("@userId", 42);
        using var reader = cmd.ExecuteReader();
        reader.Read();
        return new BenchmarkUser(
            reader.GetFieldValue<long>(0),
            reader.GetFieldValue<string>(1),
            reader.IsDBNull(2) ? null : reader.GetFieldValue<string>(2),
            reader.GetFieldValue<long>(3),
            reader.GetFieldValue<long>(4));
    }

    [Benchmark(Description = "C: Raw + GetOrdinal x5 + typed getters (ordinal cost)")]
    public BenchmarkUser C_Raw_GetOrdinal_TypedGetters()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = OrdinalSql;
        cmd.Parameters.AddWithValue("@userId", 42);
        using var reader = cmd.ExecuteReader();
        var idOrd = reader.GetOrdinal("id");
        var nameOrd = reader.GetOrdinal("name");
        var emailOrd = reader.GetOrdinal("email");
        var ageOrd = reader.GetOrdinal("age");
        var activeOrd = reader.GetOrdinal("active");
        reader.Read();
        return new BenchmarkUser(
            reader.GetInt64(idOrd),
            reader.GetString(nameOrd),
            reader.IsDBNull(emailOrd) ? null : reader.GetString(emailOrd),
            reader.GetInt64(ageOrd),
            reader.GetInt64(activeOrd));
    }

    [Benchmark(Description = "D: Raw + ordinal int[] + GetFieldValue<T> (matches AOT path)")]
    public BenchmarkUser D_Raw_OrdinalArray_GetFieldValue()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = OrdinalSql;
        cmd.Parameters.AddWithValue("@userId", 42);
        using var reader = cmd.ExecuteReader();
        var ordinals = new[]
        {
            reader.GetOrdinal("id"),
            reader.GetOrdinal("name"),
            reader.GetOrdinal("email"),
            reader.GetOrdinal("age"),
            reader.GetOrdinal("active"),
        };
        reader.Read();
        return new BenchmarkUser(
            reader.GetFieldValue<long>(ordinals[0]),
            reader.GetFieldValue<string>(ordinals[1]),
            reader.IsDBNull(ordinals[2]) ? null : reader.GetFieldValue<string>(ordinals[2]),
            reader.GetFieldValue<long>(ordinals[3]),
            reader.GetFieldValue<long>(ordinals[4]));
    }

    [Benchmark(Description = "E: StringThing QueryStringSingle (full AOT path)")]
    public BenchmarkUser E_StringThing_QueryStringSingle()
    {
        var userId = 42;
        return _connection.QueryStringSingle<BenchmarkUser>(
            $"SELECT id AS Id, name AS Name, email AS Email, age AS Age, active AS Active FROM users WHERE id = {userId}");
    }
}
