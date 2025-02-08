using System.Reflection;
using BenchmarkDotNet.Running;
using EventStorage.Benchmarks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// initialize your db with a bunch of streams before running benchmarks
// comment out this portion once done and uncomment benchmark runner line 15
var builder = Host.CreateApplicationBuilder(args);
builder.Services.ConfigureContainer();
builder.Services.AddHostedService<InitDb>();
using var host = builder.Build();
await host.RunAsync();

// BenchmarkRunner.Run(Assembly.GetExecutingAssembly());
// BenchmarkRunner.Run(Assembly.GetExecutingAssembly(), new DebugInProcessConfig());