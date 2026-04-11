using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.Sqlite;
using StringThing.Sqlite;
using StringThing.Sqlite.Dapper;

namespace StringThing.Benchmarks;

[MemoryDiagnoser]
public class InListBenchmarks
{
    private SqliteConnection _connection = null!;

    private static readonly string _prebuiltSql10 =
        "SELECT id, name, email FROM users WHERE id IN ("
        + string.Join(", ", Enumerable.Range(0, 10).Select(i => $"@p{i}"))
        + ")";

    private static readonly string _prebuiltSql100 =
        "SELECT id, name, email FROM users WHERE id IN ("
        + string.Join(", ", Enumerable.Range(0, 100).Select(i => $"@p{i}"))
        + ")";

    private int[] _tenItems = null!;
    private int[] _hundredItems = null!;
    private List<int> _tenItemsList = null!;
    private List<int> _hundredItemsList = null!;

    public record User(long Id, string Name, string? Email);

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
                email TEXT
            )
            """;
        create.ExecuteNonQuery();

        for (var i = 1; i <= 100; i++)
        {
            using var insert = _connection.CreateCommand();
            insert.CommandText = $"INSERT INTO users (id, name, email) VALUES ({i}, 'user_{i}', 'user_{i}@example.com')";
            insert.ExecuteNonQuery();
        }

        _tenItems = Enumerable.Range(1, 10).ToArray();
        _hundredItems = Enumerable.Range(1, 100).ToArray();
        _tenItemsList = _tenItems.ToList();
        _hundredItemsList = _hundredItems.ToList();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection.Dispose();
    }

    // --- 10 items ---

    [Benchmark(Description = "Raw: IN 10 (prebuilt SQL)")]
    public List<User> Raw_InList_Ten()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _prebuiltSql10;
        for (var i = 0; i < 10; i++)
            cmd.Parameters.AddWithValue($"@p{i}", _tenItems[i]);
        using var reader = cmd.ExecuteReader();
        var results = new List<User>();
        while (reader.Read())
            results.Add(new User(reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
        return results;
    }

    [Benchmark(Description = "Dapper: IN 10")]
    public List<User> Dapper_InList_Ten()
    {
        return _connection.Query<User>(
            "SELECT id, name, email FROM users WHERE id IN @ids",
            new { ids = _tenItemsList }).ToList();
    }

    [Benchmark(Description = "StringThing: IN 10")]
    public List<User> StringThing_InList_Ten()
    {
        return _connection.QueryString<User>(
            $"SELECT id, name, email FROM users WHERE id IN {SqliteSql.InList([.. _tenItems])}").ToList();
    }

    // --- 100 items ---

    [Benchmark(Description = "Raw: IN 100 (prebuilt SQL)")]
    public List<User> Raw_InList_Hundred()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _prebuiltSql100;
        for (var i = 0; i < 100; i++)
            cmd.Parameters.AddWithValue($"@p{i}", _hundredItems[i]);
        using var reader = cmd.ExecuteReader();
        var results = new List<User>();
        while (reader.Read())
            results.Add(new User(reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
        return results;
    }

    [Benchmark(Description = "Dapper: IN 100")]
    public List<User> Dapper_InList_Hundred()
    {
        return _connection.Query<User>(
            "SELECT id, name, email FROM users WHERE id IN @ids",
            new { ids = _hundredItemsList }).ToList();
    }

    [Benchmark(Description = "StringThing: IN 100")]
    public List<User> StringThing_InList_Hundred()
    {
        return _connection.QueryString<User>(
            $"SELECT id, name, email FROM users WHERE id IN {SqliteSql.InList([.. _hundredItems])}").ToList();
    }
}
