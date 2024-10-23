// using Microsoft.Extensions.Logging;
// using Redis.OM;

// namespace EventStorage.Repositories.Redis;

// public class RedisService<T>(RedisConnectionProvider redis, ILogger<RedisService<T>> logger) 
//     : IRedisService<T>
// {
//     // private readonly IRedisCollection<T> _collection =  redis.RedisCollection<T>();
//     public async Task CreateIndex()
//     {
//         try
//         {
//             await redis.Connection.CreateIndexAsync(typeof(T));
//         }
//         catch (Exception e)
//         {
//             logger.LogInformation($"Faild creating index for {typeof(T).Name}. {e.Message}");
//             throw;
//         }
//     }
// }