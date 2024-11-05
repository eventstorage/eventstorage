namespace EventStorage.Projections;

public interface IProjection
{
    ProjectionMode Mode { get; }
    ProjectionConfiguration Configuration {  get; }
}
public abstract class Projection : IProjection
{
    public ProjectionMode Mode { get; set; }
    public ProjectionConfiguration Configuration { get; set; } = new();
}
public interface IProjection<M>
{
    ProjectionMode Mode { get; }
    ProjectionConfiguration Configuration {  get; }
}
public class Projection<M> : Projection, IProjection<M>;



// namespace EventStorage.Projections;

// public interface IProjection
// {
//     ProjectionMode Mode { get; }
//     ProjectionConfiguration Configuration {  get; }
// }
// public abstract class Projection : IProjection
// {
//     public abstract ProjectionMode Mode { get; set; }
//     public abstract ProjectionConfiguration Configuration { get; set; }
// }
// public interface IProjection<M>;
// public class Projection<M> : Projection, IProjection<M>
// {
//     // private ProjectionMode _mode;
//     // private DestinationConfiguration _destination = default!;
//     public override ProjectionMode Mode {  get; set; }
//     public override ProjectionConfiguration Configuration { get; set; } = default!;
// }