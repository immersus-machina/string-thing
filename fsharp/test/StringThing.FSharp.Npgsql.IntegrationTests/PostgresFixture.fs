namespace StringThing.FSharp.Npgsql.IntegrationTests

open System.Threading.Tasks
open Testcontainers.PostgreSql
open Xunit

/// xUnit class fixture that boots a PostgreSQL container once for the test class
/// and tears it down at the end. Image is pinned to `postgres:17` to match the
/// C# integration test suite.
type PostgresFixture() =
    let container = PostgreSqlBuilder().WithImage("postgres:17").Build()
    let mutable connectionString = ""

    member _.ConnectionString = connectionString

    interface IAsyncLifetime with
        member _.InitializeAsync() : ValueTask =
            ValueTask(
                task {
                    do! container.StartAsync()
                    connectionString <- container.GetConnectionString()
                } :> Task)

        member _.DisposeAsync() : ValueTask =
            container.DisposeAsync()
