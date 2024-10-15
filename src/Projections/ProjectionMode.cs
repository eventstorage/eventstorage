namespace EventStorage.Projections;

public enum ProjectionMode
{
    Consistent = 0,
    Async = 1,
    Runtime = 2,
}