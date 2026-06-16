module StringThing.FSharp.Sqlite.IntegrationTests.Tests

#nowarn "3391"

open System
open Microsoft.Data.Sqlite
open StringThing.FSharp
open StringThing.FSharp.Sqlite
open Xunit

type User =
    { Id: int64
      Name: string
      Email: string option
      Active: bool }

let userRow : RowReader<User> =
    row {
        let! id     = Row.int64 "id"
        and! name   = Row.string "name"
        and! email  = Row.stringOption "email"
        and! active = Row.bool "active"
        return { Id = id; Name = name; Email = email; Active = active }
    }

let createDatabase () =
    let connection = new SqliteConnection("Data Source=:memory:")
    connection.Open()
    use create = connection.CreateCommand()
    create.CommandText <-
        "CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT NOT NULL, email TEXT, active INTEGER NOT NULL)"
    create.ExecuteNonQuery() |> ignore
    use insert = connection.CreateCommand()
    insert.CommandText <-
        "INSERT INTO users (id, name, email, active) VALUES \
         (1, 'alice', 'alice@example.com', 1), \
         (2, 'bob', NULL, 1), \
         (3, 'carol', 'carol@example.com', 0)"
    insert.ExecuteNonQuery() |> ignore
    connection

// -- Core query path --

[<Fact>]
let ``QueryStringSingle materializes a record via row CE`` () =
    use connection = createDatabase ()
    let userId = 1L

    let user =
        connection.QueryStringSingle userRow $"""
            SELECT id, name, email, active
            FROM users
            WHERE id = {userId}
            """

    Assert.Equal(1L, user.Id)
    Assert.Equal("alice", user.Name)
    Assert.Equal(Some "alice@example.com", user.Email)
    Assert.True(user.Active)

[<Fact>]
let ``QueryString returns lazy seq of records`` () =
    use connection = createDatabase ()
    let maxId = 3L

    let users =
        connection.QueryString userRow $"""
            SELECT id, name, email, active
            FROM users
            WHERE id <= {maxId}
            ORDER BY id
            """
        |> Seq.toList

    Assert.Equal(3, users.Length)
    Assert.Equal("alice", users.[0].Name)
    Assert.Equal(None, users.[1].Email)
    Assert.False(users.[2].Active)

[<Fact>]
let ``string option None inserts as NULL and round-trips as None`` () =
    use connection = createDatabase ()
    let id = 99L
    let name = "dave"
    let email : string option = None
    let active = true

    let inserted =
        connection.ExecuteString $"""
            INSERT INTO users (id, name, email, active)
            VALUES ({id}, {name}, {email}, {active})
            """

    Assert.Equal(1, inserted)

    let user =
        connection.QueryStringSingle userRow $"""
            SELECT id, name, email, active
            FROM users
            WHERE id = {id}
            """

    Assert.Equal(None, user.Email)

[<Fact>]
let ``QueryStringSingleOrDefault returns None for missing row`` () =
    use connection = createDatabase ()
    let id = 999L

    let user =
        connection.QueryStringSingleOrDefault userRow $"""
            SELECT id, name, email, active
            FROM users
            WHERE id = {id}
            """

    Assert.Equal(None, user)

// -- Scalar readers --

[<Fact>]
let ``Row.scalar reads a single int64 scalar`` () =
    use connection = createDatabase ()

    let count =
        connection.QueryStringSingle Row.scalar $"""
            SELECT COUNT(*) FROM users
            """

    Assert.Equal(3L, count)

[<Fact>]
let ``Row.scalar reads a single string scalar with a parameter`` () =
    use connection = createDatabase ()
    let userId = 1L

    let name : string =
        connection.QueryStringSingle Row.scalar $"""
            SELECT name FROM users WHERE id = {userId}
            """

    Assert.Equal("alice", name)

[<Fact>]
let ``Row.scalarOption returns None for NULL column`` () =
    use connection = createDatabase ()
    let userId = 2L

    let email : string option =
        connection.QueryStringSingle Row.scalarOption $"""
            SELECT email FROM users WHERE id = {userId}
            """

    Assert.Equal(None, email)

[<Fact>]
let ``QueryString with scalar returns a list of values`` () =
    use connection = createDatabase ()
    let maxId = 3L

    let names : string list =
        connection.QueryString Row.scalar $"""
            SELECT name
            FROM users
            WHERE id <= {maxId}
            ORDER BY id
            """
        |> Seq.toList

    Assert.Equal<string list>(["alice"; "bob"; "carol"], names)

// -- Fragment composition via SqlFragment --

[<Fact>]
let ``SqlFragment splices its elements when embedded in an outer $""`` () =
    use connection = createDatabase ()
    let minId = 1L
    let active = true

    let filter = Sqlite.fragment $"id > {minId} AND active = {active}"

    let users =
        connection.QueryString userRow $"""
            SELECT id, name, email, active
            FROM users
            WHERE {filter}
            ORDER BY id
            """
        |> Seq.toList

    Assert.Equal(1, users.Length)
    Assert.Equal("bob", users.[0].Name)

[<Fact>]
let ``SqlStatement built via Sqlite.statement is accepted by connection methods`` () =
    use connection = createDatabase ()
    let userId = 2L

    let statement = Sqlite.statement $"""
        SELECT id, name, email, active
        FROM users
        WHERE id = {userId}
        """

    let user = connection.QueryStringSingle userRow statement

    Assert.Equal(2L, user.Id)
    Assert.Equal("bob", user.Name)

// -- Sqlite.unsafe --

[<Fact>]
let ``Sqlite.unsafe splices raw SQL`` () =
    use connection = createDatabase ()
    let tableName = Sqlite.unsafe "users"
    let userId = 3L

    let user =
        connection.QueryStringSingle userRow $"""
            SELECT id, name, email, active
            FROM {tableName}
            WHERE id = {userId}
            """

    Assert.Equal("carol", user.Name)

// -- Sqlite.inList --

[<Fact>]
let ``Sqlite.inList expands a sequence into parameterized IN clause`` () =
    use connection = createDatabase ()
    let ids = [1L; 3L]

    let users =
        connection.QueryString userRow $"""
            SELECT id, name, email, active
            FROM users
            WHERE id IN {Sqlite.inList ids}
            ORDER BY id
            """
        |> Seq.toList

    Assert.Equal(2, users.Length)
    Assert.Equal("alice", users.[0].Name)
    Assert.Equal("carol", users.[1].Name)

[<Fact>]
let ``Sqlite.inList throws on empty sequence`` () =
    Assert.Throws<System.ArgumentException>(fun () ->
        Sqlite.inList ([] : int64 list) |> ignore) |> ignore

// -- Sqlite.insertRows --

type InsertUser = { Id: int64; Name: string; Email: string option }

let insertUserRow (u: InsertUser) : SqliteFragment =
    Sqlite.fragment $"({u.Id}, {u.Name}, {u.Email}, 1)"

[<Fact>]
let ``Sqlite.insertRows composes multi-row VALUES`` () =
    use connection = createDatabase ()

    let newUsers = [
        { Id = 50L; Name = "eve"; Email = Some "eve@example.com" }
        { Id = 51L; Name = "frank"; Email = None }
        { Id = 52L; Name = "grace"; Email = Some "grace@example.com" }
    ]

    let inserted =
        connection.ExecuteString $"""
            INSERT INTO users (id, name, email, active)
            VALUES {Sqlite.insertRows insertUserRow newUsers}
            """

    Assert.Equal(3, inserted)

    let minId = 50L
    let maxId = 52L
    let users =
        connection.QueryString userRow $"""
            SELECT id, name, email, active
            FROM users
            WHERE id >= {minId} AND id <= {maxId}
            ORDER BY id
            """
        |> Seq.toList

    Assert.Equal(3, users.Length)
    Assert.Equal("eve", users.[0].Name)
    Assert.Equal(None, users.[1].Email)
    Assert.Equal(Some "grace@example.com", users.[2].Email)

[<Fact>]
let ``Sqlite.insertRows throws on empty sequence`` () =
    Assert.Throws<System.ArgumentException>(fun () ->
        Sqlite.insertRows insertUserRow [] |> ignore) |> ignore

[<Fact>]
let ``Unsupported parameter type throws helpful runtime error`` () =
    use connection = createDatabase ()
    let weird : System.Net.IPAddress = System.Net.IPAddress.Loopback

    let ex =
        Assert.Throws<System.InvalidOperationException>(fun () ->
            connection.ExecuteString $"""
                INSERT INTO users (id, name, email, active)
                VALUES (100, {weird}, NULL, 1)
                """
            |> ignore)

    Assert.Contains("Unsupported parameter type", ex.Message)
    Assert.Contains("IPAddress", ex.Message)
