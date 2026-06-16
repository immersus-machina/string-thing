namespace StringThing.FSharp.SqlClient.IntegrationTests

open System.Threading.Tasks
open Testcontainers.MsSql
open Xunit

/// xUnit class fixture that boots a SQL Server (mssql) container once for the test class
/// and tears it down at the end.
type SqlServerFixture() =
    let container =
        MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build()
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
