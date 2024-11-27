using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run(Assembly.GetExecutingAssembly());
// BenchmarkRunner.Run(Assembly.GetExecutingAssembly(), new DebugInProcessConfig());