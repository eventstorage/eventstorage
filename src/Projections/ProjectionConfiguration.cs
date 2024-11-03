namespace EventStorage.Projections;

public class ProjectionConfiguration
{
    public ProjectionStore Store { get; set; }
    public string ConnectionString { get; set; } = "redis://localhost:6379";
}
public enum ProjectionStore
{
    Selected,
    Redis,
}