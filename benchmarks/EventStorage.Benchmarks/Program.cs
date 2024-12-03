using System.Reflection;
using BenchmarkDotNet.Running;
using EventStorage.Benchmarks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// initialize your db with 10k streams before running benchmarks
// pressing ctr+c stops initialization and starts benchmarking
var builder = Host.CreateApplicationBuilder(args);
builder.Services.ConfigureContainer();
builder.Services.AddHostedService<InitDb>();
using var host = builder.Build();
await host.RunAsync();


BenchmarkRunner.Run(Assembly.GetExecutingAssembly());
// BenchmarkRunner.Run(Assembly.GetExecutingAssembly(), new DebugInProcessConfig());