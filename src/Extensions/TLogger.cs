using Microsoft.Extensions.Logging;

namespace EventStorage.Extensions;

public static class TLogger
{
    private static ILoggerFactory _factory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole()
        .AddDebug();
    });
    public static ILogger Create<T>() => _factory.CreateLogger<T>();
}