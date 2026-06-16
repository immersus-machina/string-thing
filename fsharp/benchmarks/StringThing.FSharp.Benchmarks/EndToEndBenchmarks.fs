namespace StringThing.FSharp.Benchmarks

open System.Threading
open BenchmarkDotNet.Attributes
open Microsoft.Data.Sqlite
open StringThing.FSharp
open StringThing.FSharp.Sqlite

type BenchmarkUser =
    { Id: int64
      Name: string
      Email: string option
      Age: int64
      Active: int64 }

module BenchmarkUser =
    let row : RowReader<BenchmarkUser> =
        row {
            let! id     = Row.int64 "id"
            and! name   = Row.string "name"
            and! email  = Row.stringOption "email"
            and! age    = Row.int64 "age"
            and! active = Row.int64 "active"
            return { Id = id; Name = name; Email = email; Age = age; Active = active }
        }

[<MemoryDiagnoser>]
type EndToEndBenchmarks() =

    let mutable connection : SqliteConnection = Unchecked.defaultof<_>
    let mutable insertCounter = 1000

    [<GlobalSetup>]
    member _.Setup() =
        connection <- new SqliteConnection("Data Source=:memory:")
        connection.Open()

        use create = connection.CreateCommand()
        create.CommandText <-
            "CREATE TABLE users ( \
                id INTEGER PRIMARY KEY, \
                name TEXT NOT NULL, \
                email TEXT, \
                age INTEGER NOT NULL, \
                active INTEGER NOT NULL)"
        create.ExecuteNonQuery() |> ignore

        for i = 1 to 100 do
            use insert = connection.CreateCommand()
            insert.CommandText <-
                sprintf "INSERT INTO users (id, name, email, age, active) VALUES (%d, 'user_%d', 'user_%d@example.com', %d, %d)"
                    i i i (20 + i % 50) (if i % 3 = 0 then 0 else 1)
            insert.ExecuteNonQuery() |> ignore

    [<GlobalCleanup>]
    member _.Cleanup() =
        connection.Dispose()

    // --- Zero-parameter scalar ---

    [<Benchmark(Description = "Raw: ExecuteScalar 0 param")>]
    member _.Raw_ExecuteScalar_0Param() : int64 =
        use cmd = connection.CreateCommand()
        cmd.CommandText <- "SELECT COUNT(*) FROM users"
        cmd.ExecuteScalar() :?> int64

    [<Benchmark(Description = "FSharp: ExecuteStringScalar 0 param")>]
    member _.FSharp_ExecuteScalar_0Param() : int64 =
        let result =
            connection.ExecuteStringScalar $"""
                SELECT COUNT(*) FROM users
                """
        match result with
        | Some v -> v :?> int64
        | None -> 0L

    // --- QuerySingle 1 param ---

    [<Benchmark(Description = "Raw: QuerySingle 1 param")>]
    member _.Raw_QuerySingle_1Param() : BenchmarkUser =
        use cmd = connection.CreateCommand()
        cmd.CommandText <- "SELECT id, name, email, age, active FROM users WHERE id = @userId"
        cmd.Parameters.AddWithValue("@userId", 42) |> ignore
        use reader = cmd.ExecuteReader()
        reader.Read() |> ignore
        { Id = reader.GetInt64(0)
          Name = reader.GetString(1)
          Email = if reader.IsDBNull(2) then None else Some (reader.GetString(2))
          Age = reader.GetInt64(3)
          Active = reader.GetInt64(4) }

    [<Benchmark(Description = "FSharp: QueryStringSingle 1 param")>]
    member _.FSharp_QuerySingle_1Param() : BenchmarkUser =
        let userId = 42L
        connection.QueryStringSingle BenchmarkUser.row $"""
            SELECT id, name, email, age, active
            FROM users
            WHERE id = {userId}
            """

    // --- QuerySingle 2 params ---

    [<Benchmark(Description = "Raw: Query 2 params")>]
    member _.Raw_Query_2Params() : BenchmarkUser =
        use cmd = connection.CreateCommand()
        cmd.CommandText <-
            "SELECT id, name, email, age, active FROM users \
             WHERE age > @minAge AND active = @active \
             ORDER BY id LIMIT 1"
        cmd.Parameters.AddWithValue("@minAge", 30) |> ignore
        cmd.Parameters.AddWithValue("@active", 1) |> ignore
        use reader = cmd.ExecuteReader()
        reader.Read() |> ignore
        { Id = reader.GetInt64(0)
          Name = reader.GetString(1)
          Email = if reader.IsDBNull(2) then None else Some (reader.GetString(2))
          Age = reader.GetInt64(3)
          Active = reader.GetInt64(4) }

    [<Benchmark(Description = "FSharp: QueryString 2 params")>]
    member _.FSharp_Query_2Params() : BenchmarkUser =
        let minAge = 30L
        let active = 1L
        connection.QueryStringSingle BenchmarkUser.row $"""
            SELECT id, name, email, age, active
            FROM users
            WHERE age > {minAge} AND active = {active}
            ORDER BY id
            LIMIT 1
            """

    // --- QuerySingle 5 params ---

    [<Benchmark(Description = "Raw: Query 5 params")>]
    member _.Raw_Query_5Params() : BenchmarkUser =
        use cmd = connection.CreateCommand()
        cmd.CommandText <-
            "SELECT id, name, email, age, active FROM users \
             WHERE id > @minId AND id < @maxId AND age >= @minAge AND age <= @maxAge AND active = @active \
             LIMIT 1"
        cmd.Parameters.AddWithValue("@minId", 10) |> ignore
        cmd.Parameters.AddWithValue("@maxId", 50) |> ignore
        cmd.Parameters.AddWithValue("@minAge", 25) |> ignore
        cmd.Parameters.AddWithValue("@maxAge", 40) |> ignore
        cmd.Parameters.AddWithValue("@active", 1) |> ignore
        use reader = cmd.ExecuteReader()
        reader.Read() |> ignore
        { Id = reader.GetInt64(0)
          Name = reader.GetString(1)
          Email = if reader.IsDBNull(2) then None else Some (reader.GetString(2))
          Age = reader.GetInt64(3)
          Active = reader.GetInt64(4) }

    [<Benchmark(Description = "FSharp: QueryString 5 params")>]
    member _.FSharp_Query_5Params() : BenchmarkUser =
        let minId = 10L
        let maxId = 50L
        let minAge = 25L
        let maxAge = 40L
        let active = 1L
        connection.QueryStringSingle BenchmarkUser.row $"""
            SELECT id, name, email, age, active
            FROM users
            WHERE id > {minId}
              AND id < {maxId}
              AND age >= {minAge}
              AND age <= {maxAge}
              AND active = {active}
            LIMIT 1
            """

    // --- Execute insert ---

    [<Benchmark(Description = "Raw: Execute insert")>]
    member _.Raw_Execute_Insert() : int =
        let id = Interlocked.Increment(&insertCounter)
        use cmd = connection.CreateCommand()
        cmd.CommandText <-
            "INSERT OR IGNORE INTO users (id, name, email, age, active) VALUES (@id, @name, @email, @age, @active)"
        cmd.Parameters.AddWithValue("@id", id) |> ignore
        cmd.Parameters.AddWithValue("@name", "bench") |> ignore
        cmd.Parameters.AddWithValue("@email", "bench@test.com") |> ignore
        cmd.Parameters.AddWithValue("@age", 30) |> ignore
        cmd.Parameters.AddWithValue("@active", 1) |> ignore
        cmd.ExecuteNonQuery()

    [<Benchmark(Description = "FSharp: ExecuteString insert")>]
    member _.FSharp_Execute_Insert() : int =
        let id = int64 (Interlocked.Increment(&insertCounter))
        let name = "bench"
        let email = "bench@test.com"
        let age = 30L
        let active = 1L
        connection.ExecuteString $"""
            INSERT OR IGNORE INTO users (id, name, email, age, active)
            VALUES ({id}, {name}, {email}, {age}, {active})
            """
