using System.Data;
using System.Runtime.Serialization;
using System.Text.Json;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Extensions;
using AsyncHandler.EventSourcing.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsyncHandler.EventSourcing.Repositories.AzureSql;

public class AzureSqlClient<T>(string connectionString, IServiceProvider sp)
    : Repository<T>(connectionString, sp),
    IAzureSqlClient<T> where T : AggregateRoot
{
    private readonly string _connectionString = connectionString;
    private readonly ILogger<AzureSqlClient<T>> _logger = sp.GetRequiredService<ILogger<AzureSqlClient<T>>>();
    public async Task InitSource()
    {
        _logger.LogInformation($"Begin initializing {nameof(AzureSqlClient)}.");
        try
        {
            using SqlConnection sqlConnection = new(_connectionString);
            using SqlCommand command = new(CreateIfNotExists, sqlConnection);
            sqlConnection.Open();
            await command.ExecuteNonQueryAsync();
        }
        catch(SqlException e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Initializing {typeof(T)} failed. {e.Message}");
            throw;
        }
    }
    public async Task<T> CreateOrRestore(string sourceId)
    {
        try
        {
            using SqlConnection sqlConnection = new(_connectionString);
            using SqlCommand command = new(GetSourceCommand, sqlConnection);
            command.Parameters.AddWithValue("@aggregateId", sourceId);
            using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.Default);
            
            _logger.LogInformation($"Restoring aggregate {typeof(T)} started.");

            var aggregate = typeof(T).CreateAggregate<T>(sourceId);
            List<SourceEvent> sourceEvents = [];
            while(await reader.ReadAsync())
            {
                var aggregateId = reader.GetString(EventSourceSchema.AggregateId);
                var aggregateType = reader.GetString(EventSourceSchema.AggregateType);
                var eventType = reader.GetString(EventSourceSchema.EventType);
                var version = reader.GetString(EventSourceSchema.Version);
                var jsonData = reader.GetString(EventSourceSchema.Data);
                var sourceEvent = JsonSerializer.Deserialize<SourceEvent>(jsonData) ??
                    throw new SerializationException($"Deserialize failure for event type {eventType}, aggregateId {aggregateId}.");
                sourceEvents.Add(sourceEvent);
            };
            aggregate.RestoreAggregate(sourceEvents);
            _logger.LogInformation($"Restoring aggregate {typeof(T)} finished.");

            return aggregate;
        }
        catch(SqlException e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Failed restoring aggregate {typeof(T)}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Failed restoring aggregate {typeof(T)}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Failed restoring aggregate {typeof(T)}. {e.Message}");
            throw;
        }
    }
    public async Task Commit(T aggregate)
    {
        var count = aggregate.PendingEvents.Count();
        _logger.LogInformation($"Preparing to commit {count} pending events for {aggregate.GetType()}");
        try
        {
            List<Task> tasks = [];
            // this is turned into a batch later
            using SqlConnection sqlConnection = new(_connectionString);
            foreach (var e in aggregate.PendingEvents)
            {
                var command = new SqlCommand(InsertSourceCommand, sqlConnection);
                command.Parameters.AddWithValue("@timeStamp", DateTime.UtcNow);
                command.Parameters.AddWithValue("@sequenceNumber", 0);
                command.Parameters.AddWithValue("@aggregateId", aggregate.AggregateId);
                command.Parameters.AddWithValue("@aggregateType", nameof(aggregate));
                command.Parameters.AddWithValue("@version", aggregate.Version);
                command.Parameters.AddWithValue("@eventType", nameof(e));
                command.Parameters.AddWithValue("@data", JsonSerializer.Serialize(e));
                command.Parameters.AddWithValue("@correlationId", e.CorrelationId);
                command.Parameters.AddWithValue("@tenantId", aggregate.TenantId);
                tasks.Add(command.ExecuteNonQueryAsync());
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
            _logger.LogInformation($"Commited {count} pending events for {aggregate.GetType()}");
        }
        catch(SqlException e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Commit failure for {aggregate.GetType()}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Commit failure for {aggregate.GetType()}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            if(_logger.IsEnabled(LogLevel.Error))
                _logger.LogError($"Commit failure for {aggregate.GetType()}. {e.Message}");
            throw;
        }
    }
}