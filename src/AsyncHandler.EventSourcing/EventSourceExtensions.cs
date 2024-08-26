
using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Projections;

namespace AsyncHandler.EventSourcing;

public static class EventSourceExtensions
{
    public static EventSourceConfiguration SelectEventSource(
        this EventSourceConfiguration configuration,
        EventSources source,
        string connectionString)
    {
        configuration.EventSource = source;
        return configuration;
    }
    public static void AddProjection<T>(
        this EventSourceConfiguration configuration,
        ProjectionMode projectionMode)
    {
        
    }
}
