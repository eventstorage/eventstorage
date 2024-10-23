namespace EventStorage.Repositories.Redis;

public interface IRedisService<T>
{
    Task CreateIndex();

}