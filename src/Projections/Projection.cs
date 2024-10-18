
namespace EventStorage.Projections;
public abstract class Projection
{
    public abstract ProjectionMode Mode { get; set; }
}
public interface IProjection;
public interface IProjection<M> : IProjection;
public class Projection<M> : Projection, IProjection<M>
{
    public override ProjectionMode Mode {  get; set; }
}