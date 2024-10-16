using EventStorage.Events;

namespace EventStorage.Projections;

public interface IProjection;
public interface IProjection<M> : IProjection
{
    M Init(SourcedEvent e);
}