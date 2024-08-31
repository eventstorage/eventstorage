using System.Data;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Extensions;
using AsyncHandler.EventSourcing.Schema;
using Azure.Core.Pipeline;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsyncHandler.EventSourcing.Repositories.AzureSql;

public class AzureSqlClient<T>(string connectionString, IServiceProvider sp)
    : ClientBase where T : AggregateRoot
{
    private readonly ILogger<AzureSqlClient<T>> _logger = sp.GetRequiredService<ILogger<AzureSqlClient<T>>>();
    public async Task InitAzureSql()
    {
        _logger.LogInformation($"Begin initializing {nameof(AzureSqlClient<T>)}.");
        using SqlConnection sqlConnection = new(connectionString);
        using SqlCommand command = new(CreateIfNotExists, sqlConnection);
        sqlConnection.Open();
        await command.ExecuteNonQueryAsync();
        _logger.LogInformation($"Initializing {nameof(AzureSqlClient<T>)} has completed.");
    }
    public async Task<T> CreateOrRestore(long? sourceId = null)
    {
        try
        {
            sourceId ??= await GetSourceId();
            using SqlConnection sqlConnection = new(connectionString);
            sqlConnection.Open();
            using SqlCommand command = new(GetSourceCommand, sqlConnection);
            command.Parameters.AddWithValue("@sourceId", sourceId);
            using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.Default);
            
            _logger.LogInformation($"Restoring aggregate {typeof(T)} started.");

            var aggregate = typeof(T).CreateAggregate<T>((long) sourceId);
            // var aggregate = (AggregateRoot)Activator.CreateInstance(typeof(T), sourceId);
            List<SourceEvent> sourceEvents = [];
            while(await reader.ReadAsync())
            {
                var typeName = reader.GetString(EventSourceSchema.Type);
                var type = ResolveEventType(typeName);

                var jsonData = reader.GetString(EventSourceSchema.Data);
                var sourceEvent = JsonSerializer.Deserialize(jsonData, type) as SourceEvent ??
                    throw new SerializationException($"Deserialize failure for event type {type}, sourceId {sourceId}.");
                sourceEvents.Add(sourceEvent);
            };
            aggregate.RestoreAggregate(sourceEvents);
            _logger.LogInformation($"Finished restoring aggregate {typeof(T)}.");

            return aggregate as T;
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
        _logger.LogInformation($"Preparing to commit {count} pending event(s) for {aggregate.GetType()}");
        try
        {
            List<Task> tasks = [];
            // this is turned into a batch later
            using SqlConnection sqlConnection = new(connectionString);
            sqlConnection.Open();
            foreach (var e in aggregate.PendingEvents)
            {
                SqlCommand command = new (InsertSourceCommand, sqlConnection);
                command.Parameters.AddWithValue("@id", Guid.NewGuid());
                command.Parameters.AddWithValue("@sourceId", aggregate.SourceId);
                command.Parameters.AddWithValue("@version", aggregate.Version);
                command.Parameters.AddWithValue("@type", e.GetType().Name);
                command.Parameters.AddWithValue("@data", JsonSerializer.Serialize(e));
                command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                command.Parameters.AddWithValue("@sourceName", nameof(T));
                command.Parameters.AddWithValue("@correlationId", e.CorrelationId ??"");
                command.Parameters.AddWithValue("@tenantId", aggregate.TenantId ??"");
                tasks.Add(command.ExecuteNonQueryAsync());
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
            _logger.LogInformation($"Committed {count} pending event(s) for {aggregate.GetType()}");
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
    private async Task<long> GetSourceId()
    {
        long sourceId = 0;
        using SqlConnection sqlConnection = new (connectionString);
        sqlConnection.Open();
        using SqlCommand sqlCommand = new (GetMaxSourceId, sqlConnection);
        var reader = await sqlCommand.ExecuteReaderAsync();
        await reader.ReadAsync();
        if(reader.HasRows)
            sourceId = (long) reader.GetValue(0);
        return sourceId + 1;
    }
}