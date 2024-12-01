using EventStorage.AggregateRoot;

namespace EventStorage.Projections;

public interface IProjection<T> where T : IEventSource
{
    ProjectionMode Mode { get; }
    ProjectionConfiguration Configuration {  get; }
}
internal interface IProjection<M, T> : IProjection<T> where T : IEventSource;
public abstract class Projection
{
    public ProjectionMode Mode { get; set; }
    public ProjectionConfiguration Configuration { get; set; } = new();
}
public class Projection<M, T> : Projection, IProjection<M, T> where T : IEventSource;