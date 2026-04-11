using BenchmarkDotNet.Running;
using StringThing.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(CommandCreationBenchmarks).Assembly).Run(args);
