namespace EventStorage.Projections;

public class DestinationConfiguration
{
    public DestinationStore Store { get; set; }
    public string ConnectionString { get; set; } = "redis://localhost:6379";
}
public enum DestinationStore
{
    Selected,
    Redis,
}