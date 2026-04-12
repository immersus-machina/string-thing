# StringThing Benchmarks

BenchmarkDotNet benchmarks comparing StringThing against raw ADO.NET and Dapper on SQLite in-memory.

## Running

```
dotnet run -c Release -- -f "*.EndToEndBenchmarks.*" "*.InListBenchmarks.*" "*.CommandCreationBenchmarks.*"
```

Or run a specific suite:

```
dotnet run -c Release -- -f "*.EndToEndBenchmarks.*"
dotnet run -c Release -- -f "*.InListBenchmarks.*"
dotnet run -c Release -- -f "*.CommandCreationBenchmarks.*"
dotnet run -c Release -- -f "*.InsertRowsBenchmarks.*"
```

## Suites

| File | What it measures |
|------|-----------------|
| `CommandCreationBenchmarks.cs` | Pure construction overhead — StringThing handler vs raw `NpgsqlCommand`. No database call. |
| `EndToEndBenchmarks.cs` | Full round-trip — query/insert against SQLite in-memory. Raw vs Dapper vs StringThing. |
| `InListBenchmarks.cs` | IN clause expansion — prebuilt SQL vs Dapper `IN @list` vs `SqliteSql.InList`. |
| `InsertRowsBenchmarks.cs` | Multi-row insert — single VALUES statement vs Dapper loop. |

## Methodology

All queries return one row (`LIMIT 1`) to isolate parameter binding overhead from result set materialization. The README reports overhead numbers with the raw ADO.NET baseline subtracted.
