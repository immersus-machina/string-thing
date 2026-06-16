module StringThing.FSharp.SqlClient.IntegrationTests.Tests

#nowarn "3391"

open System
open Microsoft.Data.SqlClient
open StringThing.FSharp
open StringThing.FSharp.SqlClient
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

let private resetSchema (connection: SqlConnection) =
    use drop = connection.CreateCommand()
    drop.CommandText <- "IF OBJECT_ID('users', 'U') IS NOT NULL DROP TABLE users"
    drop.ExecuteNonQuery() |> ignore

    use create = connection.CreateCommand()
    create.CommandText <-
        "CREATE TABLE users (\
            id BIGINT PRIMARY KEY, \
            name NVARCHAR(255) NOT NULL, \
            email NVARCHAR(255) NULL, \
            active BIT NOT NULL)"
    create.ExecuteNonQuery() |> ignore

    use insert = connection.CreateCommand()
    insert.CommandText <-
        "INSERT INTO users (id, name, email, active) VALUES \
         (1, N'alice', N'alice@example.com', 1), \
         (2, N'bob', NULL, 1), \
         (3, N'carol', N'carol@example.com', 0)"
    insert.ExecuteNonQuery() |> ignore

let private openConnection (fixture: SqlServerFixture) =
    let connection = new SqlConnection(fixture.ConnectionString)
    connection.Open()
    resetSchema connection
    connection

type Tests(fixture: SqlServerFixture) =
    interface IClassFixture<SqlServerFixture>

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
                SELECT COUNT_BIG(*) FROM users
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
    member _.``SqlServerFragment splices its elements when embedded in an outer $""`` () =
        use connection = openConnection fixture
        let minId = 1L
        let active = true

        let filter = SqlServer.fragment $"id > {minId} AND active = {active}"

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
    member _.``SqlServerStatement built via SqlServer.statement is accepted by connection methods`` () =
        use connection = openConnection fixture
        let userId = 2L

        let statement = SqlServer.statement $"""
            SELECT id, name, email, active
            FROM users
            WHERE id = {userId}
            """

        let user = connection.QueryStringSingle userRow statement

        Assert.Equal(2L, user.Id)
        Assert.Equal("bob", user.Name)

    // -- SqlServer.unsafe --

    [<Fact>]
    member _.``SqlServer.unsafe splices raw SQL`` () =
        use connection = openConnection fixture
        let tableName = SqlServer.unsafe "users"
        let userId = 3L

        let user =
            connection.QueryStringSingle userRow $"""
                SELECT id, name, email, active
                FROM {tableName}
                WHERE id = {userId}
                """

        Assert.Equal("carol", user.Name)

    // -- SqlServer.inList --

    [<Fact>]
    member _.``SqlServer.inList expands a sequence into parameterized IN clause`` () =
        use connection = openConnection fixture
        let ids = [1L; 3L]

        let users =
            connection.QueryString userRow $"""
                SELECT id, name, email, active
                FROM users
                WHERE id IN {SqlServer.inList ids}
                ORDER BY id
                """
            |> Seq.toList

        Assert.Equal(2, users.Length)
        Assert.Equal("alice", users.[0].Name)
        Assert.Equal("carol", users.[1].Name)

    // -- SqlServer.insertRows --

    [<Fact>]
    member _.``SqlServer.insertRows composes multi-row VALUES`` () =
        use connection = openConnection fixture

        let insertUser (u: {| Id: int64; Name: string; Email: string option |}) : SqlServerFragment =
            SqlServer.fragment $"({u.Id}, {u.Name}, {u.Email}, 1)"

        let newUsers = [
            {| Id = 50L; Name = "eve"; Email = Some "eve@example.com" |}
            {| Id = 51L; Name = "frank"; Email = None |}
            {| Id = 52L; Name = "grace"; Email = Some "grace@example.com" |}
        ]

        let inserted =
            connection.ExecuteString $"""
                INSERT INTO users (id, name, email, active)
                VALUES {SqlServer.insertRows insertUser newUsers}
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
