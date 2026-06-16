module StringThing.FSharp.Npgsql.IntegrationTests.Tests

#nowarn "3391"

open System
open Npgsql
open StringThing.FSharp
open StringThing.FSharp.Npgsql
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

let private resetSchema (connection: NpgsqlConnection) =
    use drop = connection.CreateCommand()
    drop.CommandText <- "DROP TABLE IF EXISTS users"
    drop.ExecuteNonQuery() |> ignore

    use create = connection.CreateCommand()
    create.CommandText <-
        "CREATE TABLE users (\
            id BIGINT PRIMARY KEY, \
            name TEXT NOT NULL, \
            email TEXT NULL, \
            active BOOLEAN NOT NULL)"
    create.ExecuteNonQuery() |> ignore

    use insert = connection.CreateCommand()
    insert.CommandText <-
        "INSERT INTO users (id, name, email, active) VALUES \
         (1, 'alice', 'alice@example.com', true), \
         (2, 'bob', NULL, true), \
         (3, 'carol', 'carol@example.com', false)"
    insert.ExecuteNonQuery() |> ignore

let private openConnection (fixture: PostgresFixture) =
    let connection = new NpgsqlConnection(fixture.ConnectionString)
    connection.Open()
    resetSchema connection
    connection

type Tests(fixture: PostgresFixture) =
    interface IClassFixture<PostgresFixture>

    // -- Core query path --

    [<Fact>]
    member _.``QueryStringSingle materializes a record via row CE`` () =
        use connection = openConnection fixture
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
    member _.``QueryString returns lazy seq of records`` () =
        use connection = openConnection fixture
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
    member _.``string option None inserts as NULL and round-trips as None`` () =
        use connection = openConnection fixture
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
    member _.``QueryStringSingleOrDefault returns None for missing row`` () =
        use connection = openConnection fixture
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
    member _.``Row.scalar reads a single int64 scalar`` () =
        use connection = openConnection fixture

        let count =
            connection.QueryStringSingle Row.scalar $"""
                SELECT COUNT(*) FROM users
                """

        Assert.Equal(3L, count)

    [<Fact>]
    member _.``Row.scalar reads a single string scalar with a parameter`` () =
        use connection = openConnection fixture
        let userId = 1L

        let name : string =
            connection.QueryStringSingle Row.scalar $"""
                SELECT name FROM users WHERE id = {userId}
                """

        Assert.Equal("alice", name)

    // -- Fragment composition --

    [<Fact>]
    member _.``PostgresFragment splices its elements when embedded in an outer $""`` () =
        use connection = openConnection fixture
        let minId = 1L
        let active = true

        let filter = Postgres.fragment $"id > {minId} AND active = {active}"

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
    member _.``PostgresStatement built via Postgres.statement is accepted by connection methods`` () =
        use connection = openConnection fixture
        let userId = 2L

        let statement = Postgres.statement $"""
            SELECT id, name, email, active
            FROM users
            WHERE id = {userId}
            """

        let user = connection.QueryStringSingle userRow statement

        Assert.Equal(2L, user.Id)
        Assert.Equal("bob", user.Name)

    // -- Postgres.unsafe --

    [<Fact>]
    member _.``Postgres.unsafe splices raw SQL`` () =
        use connection = openConnection fixture
        let tableName = Postgres.unsafe "users"
        let userId = 3L

        let user =
            connection.QueryStringSingle userRow $"""
                SELECT id, name, email, active
                FROM {tableName}
                WHERE id = {userId}
                """

        Assert.Equal("carol", user.Name)

    // -- Postgres.inList --

    [<Fact>]
    member _.``Postgres.inList expands a sequence into parameterized IN clause`` () =
        use connection = openConnection fixture
        let ids = [1L; 3L]

        let users =
            connection.QueryString userRow $"""
                SELECT id, name, email, active
                FROM users
                WHERE id IN {Postgres.inList ids}
                ORDER BY id
                """
            |> Seq.toList

        Assert.Equal(2, users.Length)
        Assert.Equal("alice", users.[0].Name)
        Assert.Equal("carol", users.[1].Name)

    // -- Postgres.insertRows --

    [<Fact>]
    member _.``Postgres.insertRows composes multi-row VALUES`` () =
        use connection = openConnection fixture

        let insertUser (u: {| Id: int64; Name: string; Email: string option |}) : PostgresFragment =
            Postgres.fragment $"({u.Id}, {u.Name}, {u.Email}, true)"

        let newUsers = [
            {| Id = 50L; Name = "eve"; Email = Some "eve@example.com" |}
            {| Id = 51L; Name = "frank"; Email = None |}
            {| Id = 52L; Name = "grace"; Email = Some "grace@example.com" |}
        ]

        let inserted =
            connection.ExecuteString $"""
                INSERT INTO users (id, name, email, active)
                VALUES {Postgres.insertRows insertUser newUsers}
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

    // -- Postgres array parameters (provider-specific) --

    [<Fact>]
    member _.``int64 array parameter binds as PostgreSQL bigint array`` () =
        use connection = openConnection fixture
        let ids : int64[] = [| 1L; 3L |]

        let users =
            connection.QueryString userRow $"""
                SELECT id, name, email, active
                FROM users
                WHERE id = ANY({ids})
                ORDER BY id
                """
            |> Seq.toList

        Assert.Equal(2, users.Length)
        Assert.Equal("alice", users.[0].Name)
        Assert.Equal("carol", users.[1].Name)
