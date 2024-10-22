
namespace EventStorage.Projections;

public interface IProjection
{
    ProjectionMode Mode { get; set;}
}
public abstract class Projection : IProjection
{
    public abstract ProjectionMode Mode { get; set; }
}
public interface IProjection<M>;
public class Projection<M> : Projection, IProjection<M>
{
    public override ProjectionMode Mode {  get; set; }
}