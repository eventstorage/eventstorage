namespace EventStorage.Projections;

public interface IProjection
{
    ProjectionMode Mode { get; }
    DestinationConfiguration Destination {  get; }
}
public abstract class Projection : IProjection
{
    public abstract ProjectionMode Mode { get; set; }
    public abstract DestinationConfiguration Destination { get; set; }
}
public interface IProjection<M>;
public class Projection<M> : Projection, IProjection<M>
{
    // private ProjectionMode _mode;
    // private DestinationConfiguration _destination = default!;
    public override ProjectionMode Mode {  get; set; }
    public override DestinationConfiguration Destination { get; set; } = default!;
}