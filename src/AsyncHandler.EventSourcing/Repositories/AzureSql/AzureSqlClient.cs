using System.Data;
using System.Runtime.Serialization;
using System.Text.Json;
using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Extensions;
using AsyncHandler.EventSourcing.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsyncHandler.EventSourcing.Repositories.AzureSql;

public class AzureSqlClient<T>(string conn, IServiceProvider sp, EventSources source) 
    : ClientBase<T>(sp, source), IAzureSqlClient<T> where T : IAggregateRoot
{
    private readonly SemaphoreSlim _semaphore = new (1, 1);
    private readonly ILogger<AzureSqlClient<T>> logger = sp.GetRequiredService<ILogger<AzureSqlClient<T>>>();
    public async Task Init()
    {
        logger.LogInformation($"Begin initializing {nameof(AzureSqlClient<T>)}.");
        _semaphore.Wait();
        using SqlConnection sqlConnection = new(conn);
        sqlConnection.Open();
        using SqlCommand command = new(CreateSchemaIfNotExists, sqlConnection);
        await command.ExecuteNonQueryAsync();
        logger.LogInformation($"Finished initializing {nameof(AzureSqlClient<T>)}.");
        _semaphore.Release();
    }
    public async Task<T> CreateOrRestore(string? sourceId = null)
    {
        try
        {
            logger.LogInformation($"Restoring aggregate {typeof(T).Name} started.");

            sourceId ??= await GenerateSourceId();
            var aggregate = typeof(T).CreateAggregate<T>(sourceId);
            
            using SqlConnection sqlConnection = new(conn);
            sqlConnection.Open();
            using SqlCommand command = new(GetSourceCommand, sqlConnection);
            command.Parameters.AddWithValue("@sourceId", sourceId);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            
            List<SourcedEvent> sourcedEvents = [];
            while(await reader.ReadAsync())
            {
                LongSourceId = reader.GetInt64(EventSourceSchema.LongSourceId);
                GuidSourceId = reader.GetGuid(EventSourceSchema.GuidSourceId);

                var typeName = reader.GetString(EventSourceSchema.Type);
                var type = ResolveEventType(typeName);
                var jsonData = reader.GetString(EventSourceSchema.Data);
                var sourcedEvent = JsonSerializer.Deserialize(jsonData, type) as SourcedEvent ??
                    throw new SerializationException($"Deserialize failure for event type {type}, sourceId {sourceId}.");
                sourcedEvents.Add(sourcedEvent);
            };
            aggregate.RestoreAggregate(RestoreType.Stream, sourcedEvents.ToArray());
            logger.LogInformation($"Finished restoring aggregate {typeof(T).Name}.");

            return aggregate;
        }
        catch(SqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Failed restoring aggregate {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Failed restoring aggregate {typeof(T).Name}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Failed restoring aggregate {typeof(T).Name}. {e.Message}");
            throw;
        }
    }
    public async Task Commit(T aggregate)
    {
        var events = aggregate.PendingEvents.Count();
        logger.LogInformation($"Preparing to commit {events} pending event(s) for {aggregate.GetType().Name}");
        try
        {
            if(aggregate.PendingEvents.Any())
            {
                using SqlConnection sqlConnection = new(conn);
                sqlConnection.Open();
                using SqlCommand command = new ("", sqlConnection);
                var preparedCommand = PrepareCommand(command, aggregate);
                command.CommandText = preparedCommand[0..^1];
                await command.ExecuteNonQueryAsync();
            }
            aggregate.CommitPendingEvents();
            logger.LogInformation($"Committed {events} pending event(s) for {aggregate.GetType().Name}");
        }
        catch(SqlException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {aggregate.GetType().Name}. {e.Message}");
            throw;
        }
        catch(SerializationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {aggregate.GetType().Name}. {e.Message}");
            throw;
        }
        catch (Exception e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Commit failure for {aggregate.GetType().Name}. {e.Message}");
            throw;
        }
    }
    // this needs optimistic locking
    private async Task<string> GenerateSourceId()
    {
        using SqlConnection sqlConnection = new(conn);
        sqlConnection.Open();
        using SqlCommand sqlCommand = new (GetMaxSourceId, sqlConnection);
        var reader = await sqlCommand.ExecuteReaderAsync();
        await reader.ReadAsync();
        long longId = 0;
        if(reader.HasRows)
            longId = (long)reader.GetValue(0);
        LongSourceId = longId + 1;
        GuidSourceId = Guid.NewGuid();
        return SourceTId == TId.LongSourceId ? LongSourceId.ToString() : GuidSourceId.ToString();
    }
    private string PrepareCommand(SqlCommand command, IAggregateRoot aggregate)
    {
        int count = 0;
        var sqlCommand = InsertSourceCommand;
        foreach (var e in aggregate.PendingEvents)
        {
            sqlCommand +=
            @$"(@id{count}, @longSourceId{count}, @guidSourceId{count}, @version{count},"+
            $"@type{count}, @data{count}, @timestamp{count}, @sourceType{count},"+
            $"@tenantId{count}, @correlationId{count}, @causationId{count}),";
            command.Parameters.AddWithValue($"@id{count}", e.Id);
            command.Parameters.AddWithValue($"@longSourceId{count}", LongSourceId);
            command.Parameters.AddWithValue($"@guidSourceId{count}", GuidSourceId);
            command.Parameters.AddWithValue($"@version{count}", e.Version);
            command.Parameters.AddWithValue($"@type{count}", e.GetType().Name);
            var json = JsonSerializer.Serialize(e, e.GetType(), SerializerOptions);
            command.Parameters.AddWithValue($"@data{count}", json);
            command.Parameters.AddWithValue($"@timestamp{count}", DateTime.UtcNow);
            command.Parameters.AddWithValue($"@sourceType{count}", typeof(T).Name);
            command.Parameters.AddWithValue($"@tenantId{count}", e.TenantId ?? "Default");
            command.Parameters.AddWithValue($"@correlationId{count}", e.CorrelationId ?? "Default");
            command.Parameters.AddWithValue($"@causationId{count}", e.CausationId ?? "Default");
            count++;
        }
        return sqlCommand;
    }
}