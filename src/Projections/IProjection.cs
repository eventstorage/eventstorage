using EventStorage.AggregateRoot;

namespace EventStorage.Projections;

public interface IProjection<T>
{
    // Task<T> Project(IEventSource eventSource);
}