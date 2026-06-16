namespace StringThing.FSharp.Benchmarks

open BenchmarkDotNet.Attributes
open Microsoft.Data.Sqlite
open StringThing.FSharp
open StringThing.FSharp.Sqlite

type InListUser =
    { Id: int64
      Name: string
      Email: string option }

module InListUser =
    let row : RowReader<InListUser> =
        row {
            let! id    = Row.int64 "id"
            and! name  = Row.string "name"
            and! email = Row.stringOption "email"
            return { Id = id; Name = name; Email = email }
        }

[<MemoryDiagnoser>]
type InListBenchmarks() =

    let mutable connection : SqliteConnection = Unchecked.defaultof<_>
    let mutable tenItems : int64[] = Unchecked.defaultof<_>
    let mutable hundredItems : int64[] = Unchecked.defaultof<_>

    let prebuiltSql10 =
        "SELECT id, name, email FROM users WHERE id IN ("
        + System.String.Join(", ", [| for i in 0..9 -> sprintf "@p%d" i |])
        + ")"

    let prebuiltSql100 =
        "SELECT id, name, email FROM users WHERE id IN ("
        + System.String.Join(", ", [| for i in 0..99 -> sprintf "@p%d" i |])
        + ")"

    [<GlobalSetup>]
    member _.Setup() =
        connection <- new SqliteConnection("Data Source=:memory:")
        connection.Open()

        use create = connection.CreateCommand()
        create.CommandText <-
            "CREATE TABLE users ( \
                id INTEGER PRIMARY KEY, \
                name TEXT NOT NULL, \
                email TEXT)"
        create.ExecuteNonQuery() |> ignore

        for i = 1 to 100 do
            use insert = connection.CreateCommand()
            insert.CommandText <-
                sprintf "INSERT INTO users (id, name, email) VALUES (%d, 'user_%d', 'user_%d@example.com')" i i i
            insert.ExecuteNonQuery() |> ignore

        tenItems <- [| 1L .. 10L |]
        hundredItems <- [| 1L .. 100L |]

    [<GlobalCleanup>]
    member _.Cleanup() =
        connection.Dispose()

    [<Benchmark(Description = "Raw: IN 10 (prebuilt SQL)")>]
    member _.Raw_InList_10() : System.Collections.Generic.List<InListUser> =
        use cmd = connection.CreateCommand()
        cmd.CommandText <- prebuiltSql10
        for i = 0 to 9 do
            cmd.Parameters.AddWithValue(sprintf "@p%d" i, tenItems.[i]) |> ignore
        use reader = cmd.ExecuteReader()
        let results = System.Collections.Generic.List<InListUser>()
        while reader.Read() do
            results.Add({
                Id = reader.GetInt64(0)
                Name = reader.GetString(1)
                Email = if reader.IsDBNull(2) then None else Some (reader.GetString(2))
            })
        results

    [<Benchmark(Description = "FSharp: IN 10")>]
    member _.FSharp_InList_10() : InListUser list =
        let ids = Sqlite.inList tenItems
        connection.QueryString InListUser.row $"""
            SELECT id, name, email
            FROM users
            WHERE id IN {ids}
            """
        |> Seq.toList

    [<Benchmark(Description = "Raw: IN 100 (prebuilt SQL)")>]
    member _.Raw_InList_100() : System.Collections.Generic.List<InListUser> =
        use cmd = connection.CreateCommand()
        cmd.CommandText <- prebuiltSql100
        for i = 0 to 99 do
            cmd.Parameters.AddWithValue(sprintf "@p%d" i, hundredItems.[i]) |> ignore
        use reader = cmd.ExecuteReader()
        let results = System.Collections.Generic.List<InListUser>()
        while reader.Read() do
            results.Add({
                Id = reader.GetInt64(0)
                Name = reader.GetString(1)
                Email = if reader.IsDBNull(2) then None else Some (reader.GetString(2))
            })
        results

    [<Benchmark(Description = "FSharp: IN 100")>]
    member _.FSharp_InList_100() : InListUser list =
        let ids = Sqlite.inList hundredItems
        connection.QueryString InListUser.row $"""
            SELECT id, name, email
            FROM users
            WHERE id IN {ids}
            """
        |> Seq.toList
