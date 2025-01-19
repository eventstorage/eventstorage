using Microsoft.Extensions.Logging;

namespace EventStorage.Extensions;

public static class TLogger
{
    private static ILoggerFactory _factory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
    });
    public static ILogger Create<T>() => _factory.CreateLogger<T>();
    public static void Log(this ILogger logger, string message) => logger.LogInformation(message);
    public static void Error(this ILogger logger, string message)
    {
        if(logger.IsEnabled(LogLevel.Error))
            logger.LogError(message);
    }
}