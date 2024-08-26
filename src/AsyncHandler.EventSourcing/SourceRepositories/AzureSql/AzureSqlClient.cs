using System.Data;
using System.Runtime.Serialization;
using System.Text.Json;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Extensions;
using AsyncHandler.EventSourcing.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AsyncHandler.EventSourcing.SourceRepositories.AzureSql;

public class AzureSqlClient<T> : IAzureSqlClient<T> where T : AggregateRoot
{
    private readonly SqlConnection _sqlConnection;
    private readonly ILogger<AzureSqlClient<T>> _logger;
    private static readonly string getSourceCommand = @"SELECT * FROM [dbo].[EventSource] WHERE [AggregateId] = @AggregateId";
    private static readonly string insertSourceEvent = @"INSERT INTO [dbo].[EventSource] VALUES (@timestamp, @sequencenumber, @aggregateid, @aggregatetype, @version, @eventtype, @data, @correlationid, @tenantid)";
    public AzureSqlClient(string connectionString, ILogger<AzureSqlClient<T>> logger)
    {
        _logger = logger;
        _sqlConnection = new(connectionString);
        _sqlConnection.InitConnection();
    }
    public async Task<T> CreateOrRestore(string sourceId)
    {
        try
        {
            using SqlCommand command = new(getSourceCommand, _sqlConnection);
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
            foreach (var e in aggregate.PendingEvents)
            {
                var command = new SqlCommand(insertSourceEvent, _sqlConnection);
                command.Parameters.Add(DateTime.UtcNow);
                command.Parameters.Add(0);
                command.Parameters.Add(aggregate.AggregateId);
                command.Parameters.Add(nameof(aggregate));
                command.Parameters.Add(aggregate.Version);
                command.Parameters.Add(nameof(e));
                command.Parameters.Add(JsonSerializer.Serialize(e));
                command.Parameters.Add(e.CorrelationId);
                command.Parameters.Add(aggregate.TenantId);
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