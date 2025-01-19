namespace EventStorage.Projections;

public interface IProjection
{
    ProjectionMode Mode { get; }
    ProjectionConfiguration Configuration {  get; }
}
internal interface IProjection<M> : IProjection;
public abstract class Projection : IProjection
{
    public ProjectionMode Mode { get; set; }
    public ProjectionConfiguration Configuration { get; set; } = new();
}
public class Projection<M> : Projection, IProjection<M>;