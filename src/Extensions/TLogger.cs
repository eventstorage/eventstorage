using Microsoft.Extensions.Logging;

namespace EventStorage.Extensions;

public static class TLogger
{
    private static readonly LoggerFactory _factory = new();
    public static ILogger Create<T>() => _factory.CreateLogger<T>();
}