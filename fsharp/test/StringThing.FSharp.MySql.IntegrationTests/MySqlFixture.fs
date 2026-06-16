namespace StringThing.FSharp.MySql.IntegrationTests

open System.Threading.Tasks
open Testcontainers.MySql
open Xunit

/// xUnit class fixture that boots a MySQL container once for the test class and
/// tears it down at the end. Use via constructor injection on test classes that
/// implement `IClassFixture<MySqlFixture>`.
type MySqlFixture() =
    let container = MySqlBuilder().WithImage("mysql:8.0").Build()
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
