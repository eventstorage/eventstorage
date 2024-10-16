
using EventStorage.Events;

namespace EventStorage.Projections;

public abstract class Projection<M> : IProjection<M>
{
    public Projection(ProjectionMode mode)
    {
        _mode = mode;
    }
    public Projection()
    {
        
    }
    private ProjectionMode _mode;
    public ProjectionMode Mode {  get; set; }
    public abstract M Init(SourcedEvent e);
}