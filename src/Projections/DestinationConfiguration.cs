namespace EventStorage.Projections;

public class DestinationConfiguration
{
    public string? ConnectionString { get; set; }
    public Destination Destination { get; set; }
}
public enum Destination
{
    Selected,
    Redis
}