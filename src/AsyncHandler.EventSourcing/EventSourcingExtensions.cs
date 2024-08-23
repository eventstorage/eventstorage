
using AsyncHandler.EventSourcing.Projections;

namespace AsyncHandler.EventSourcing;

public static class EventSourcingExtensions
{
    public static EventSourceConfiguration UseDocumentMode(
        this EventSourceConfiguration configuration,
        string connectionString)
    {
        return configuration;
    }
    public static EventSourceConfiguration UseRelationalMode(
        this EventSourceConfiguration configuration,
        string connectionString)
    {
        return configuration;
    }
    public static void AddProjection<T>(
        this EventSourceConfiguration configuration,
        ProjectionMode projectionMode)
    {

    }
}
