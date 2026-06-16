module StringThing.FSharp.Benchmarks.Program

open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    BenchmarkSwitcher
        .FromAssembly(typeof<StringThing.FSharp.Benchmarks.EndToEndBenchmarks>.Assembly)
        .Run(argv)
    |> ignore
    0
