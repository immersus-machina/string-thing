using BenchmarkDotNet.Attributes;
using Npgsql;
using StringThing.Npgsql;

namespace StringThing.Benchmarks;

[MemoryDiagnoser]
public class InsertRowsBenchmarks
{
    private NpgsqlConnection _connection = null!;
    private InsertUser[] _tenRows = null!;
    private InsertUser[] _thousandRows = null!;

    private record InsertUser(int Id, string Name, string? Email) : IPostgresRow
    {
        public PostgresFragment RowValues => $"({Id}, {Name}, {Email})";
    }

    [GlobalSetup]
    public void Setup()
    {
        _connection = new NpgsqlConnection("Host=localhost");
        _tenRows = Enumerable.Range(1, 10).Select(i => new InsertUser(i, $"user_{i}", $"user_{i}@example.com")).ToArray();
        _thousandRows = Enumerable.Range(1, 1000).Select(i => new InsertUser(i, $"user_{i}", $"user_{i}@example.com")).ToArray();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection.Dispose();
    }

    // --- 10 rows ---

    [Benchmark(Description = "StringThing: InsertRows 10 rows")]
    public NpgsqlCommand StringThing_TenRows()
    {
        PostgresSql stmt = $"INSERT INTO users (id, name, email) VALUES {PostgresSql.InsertRows(_tenRows)}";
        return stmt.ToCommand(_connection);
    }

    [Benchmark(Description = "Raw Npgsql: manual VALUES 10 rows")]
    public NpgsqlCommand RawNpgsql_TenRows()
    {
        var placeholders = string.Join(", ", Enumerable.Range(0, 10).Select(i => $"(${i * 3 + 1}, ${i * 3 + 2}, ${i * 3 + 3})"));
        var cmd = new NpgsqlCommand($"INSERT INTO users (id, name, email) VALUES {placeholders}", _connection);
        for (var i = 0; i < 10; i++)
        {
            cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = _tenRows[i].Id });
            cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = _tenRows[i].Name });
            cmd.Parameters.Add(new NpgsqlParameter<string?> { TypedValue = _tenRows[i].Email });
        }
        return cmd;
    }

    // --- 1000 rows ---

    [Benchmark(Description = "StringThing: InsertRows 1000 rows")]
    public NpgsqlCommand StringThing_ThousandRows()
    {
        PostgresSql stmt = $"INSERT INTO users (id, name, email) VALUES {PostgresSql.InsertRows(_thousandRows)}";
        return stmt.ToCommand(_connection);
    }

    [Benchmark(Description = "Raw Npgsql: manual VALUES 1000 rows")]
    public NpgsqlCommand RawNpgsql_ThousandRows()
    {
        var placeholders = string.Join(", ", Enumerable.Range(0, 1000).Select(i => $"(${i * 3 + 1}, ${i * 3 + 2}, ${i * 3 + 3})"));
        var cmd = new NpgsqlCommand($"INSERT INTO users (id, name, email) VALUES {placeholders}", _connection);
        for (var i = 0; i < 1000; i++)
        {
            cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = _thousandRows[i].Id });
            cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = _thousandRows[i].Name });
            cmd.Parameters.Add(new NpgsqlParameter<string?> { TypedValue = _thousandRows[i].Email });
        }
        return cmd;
    }
}
