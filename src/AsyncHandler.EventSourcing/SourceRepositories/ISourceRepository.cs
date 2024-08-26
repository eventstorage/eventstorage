namespace AsyncHandler.EventSourcing.SourceRepositories;

public interface ISourceRepository<T>
{
    Task GetSource(string sourceId);
    Task AddSource(T source);
}