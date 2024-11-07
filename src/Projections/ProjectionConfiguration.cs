namespace EventStorage.Projections;

public class ProjectionConfiguration
{
    public ProjectionStore Store { get; private set; }
    public string ConnectionString { get; private set; } = "redis://localhost:6379";
    public ProjectionConfiguration Redis(string connection)
    {
        Store = ProjectionStore.Redis;
        ConnectionString = connection;
        return this;
    }
}
public enum ProjectionStore
{
    Selected,
    Redis,
}