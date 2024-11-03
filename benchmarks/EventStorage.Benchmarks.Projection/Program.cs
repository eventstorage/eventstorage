using BenchmarkDotNet.Running;
using EventStorage.Benchmarks.Projections;

BenchmarkRunner.Run(typeof(ProjectionBenchmarks).Assembly);