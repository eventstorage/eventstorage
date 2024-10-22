namespace EventStorage.Projections;

public class DestinationConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public DestinationStore Store { get; set; }
}
public enum DestinationStore
{
    Selected,
    Redis
}