using BenchmarkDotNet.Attributes;
using Dapper;
using Npgsql;
using StringThing.Npgsql;

namespace StringThing.Benchmarks;

[MemoryDiagnoser]
public class CommandCreationBenchmarks
{
    private NpgsqlConnection _connection = null!;
    private int _userId;
    private string _name = null!;
    private int _minAge;
    private string _status = null!;
    private decimal _minAmount;

    [GlobalSetup]
    public void Setup()
    {
        _connection = new NpgsqlConnection("Host=localhost");
        _userId = 42;
        _name = "alice";
        _minAge = 18;
        _status = "active";
        _minAmount = 100.50m;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection.Dispose();
    }

    // --- Single parameter ---

    [Benchmark(Description = "StringThing: 1 param")]
    public NpgsqlCommand StringThing_SingleParam()
    {
        PostgresSql stmt = $"SELECT * FROM users WHERE id = {_userId}";
        return stmt.ToCommand(_connection);
    }

    [Benchmark(Description = "Raw Npgsql: 1 param")]
    public NpgsqlCommand RawNpgsql_SingleParam()
    {
        var cmd = new NpgsqlCommand("SELECT * FROM users WHERE id = $1", _connection);
        cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = _userId });
        return cmd;
    }

    // --- Three parameters ---

    [Benchmark(Description = "StringThing: 3 params")]
    public NpgsqlCommand StringThing_ThreeParams()
    {
        PostgresSql stmt = $"SELECT * FROM users WHERE id = {_userId} AND name = {_name} AND age >= {_minAge}";
        return stmt.ToCommand(_connection);
    }

    [Benchmark(Description = "Raw Npgsql: 3 params")]
    public NpgsqlCommand RawNpgsql_ThreeParams()
    {
        var cmd = new NpgsqlCommand("SELECT * FROM users WHERE id = $1 AND name = $2 AND age >= $3", _connection);
        cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = _userId });
        cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = _name });
        cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = _minAge });
        return cmd;
    }

    // --- Five parameters ---

    [Benchmark(Description = "StringThing: 5 params")]
    public NpgsqlCommand StringThing_FiveParams()
    {
        PostgresSql stmt = $"SELECT * FROM users WHERE id = {_userId} AND name = {_name} AND age >= {_minAge} AND status = {_status} AND balance >= {_minAmount}";
        return stmt.ToCommand(_connection);
    }

    [Benchmark(Description = "Raw Npgsql: 5 params")]
    public NpgsqlCommand RawNpgsql_FiveParams()
    {
        var cmd = new NpgsqlCommand("SELECT * FROM users WHERE id = $1 AND name = $2 AND age >= $3 AND status = $4 AND balance >= $5", _connection);
        cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = _userId });
        cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = _name });
        cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = _minAge });
        cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = _status });
        cmd.Parameters.Add(new NpgsqlParameter<decimal> { TypedValue = _minAmount });
        return cmd;
    }

    // --- Deduplicated parameter ---

    [Benchmark(Description = "StringThing: dedup param")]
    public NpgsqlCommand StringThing_DeduplicatedParam()
    {
        PostgresSql stmt = $"SELECT * FROM users WHERE name = {_name} OR email LIKE {_name}";
        return stmt.ToCommand(_connection);
    }

    [Benchmark(Description = "Raw Npgsql: dedup param (manual)")]
    public NpgsqlCommand RawNpgsql_DeduplicatedParam()
    {
        var cmd = new NpgsqlCommand("SELECT * FROM users WHERE name = $1 OR email LIKE $1", _connection);
        cmd.Parameters.Add(new NpgsqlParameter<string> { TypedValue = _name });
        return cmd;
    }
}
