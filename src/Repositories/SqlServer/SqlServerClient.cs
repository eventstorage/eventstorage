using System.Data;
using System.Runtime.Serialization;
using System.Text.Json;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Extensions;
using EventStorage.Projections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventStorage.Repositories.SqlServer;

public class SqlServerClient<T>(string conn, IServiceProvider sp, EventStore source)
    : ClientBase<T>(sp, source), ISqlServerClient<T> where T : IEventSource
{
    private readonly SemaphoreSlim _semaphore = new (1, 1);
    private readonly ILogger logger = sp.GetRequiredService<ILogger<SqlServerClient<T>>>();
    private readonly IProjectionEngine _projection = sp.GetRequiredService<IProjectionEngine>();
    public async Task Init()
    {
        try
        {
            _semaphore.Wait();
            logger.LogInformation($"Begin initializing {nameof(SqlServerClient<T>)}.");
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            await using SqlCommand command = new(CreateSchemaIfNotExists, sqlConnection);
            command.Transaction = sqlTransaction;
            await command.ExecuteNonQueryAsync();
            foreach (var item in TProjections(t => true))
            {
                command.CommandText = CreateProjectionIfNotExists(item?.Name?? "");
                await command.ExecuteNonQueryAsync();
            }
            await sqlTransaction.CommitAsync();
            logger.LogInformation($"Finished initializing {nameof(SqlServerClient<T>)}.");
            _semaphore.Release();
        }
        catch(SqlException e)
        {
            logger.LogInformation($"Failed initializing {nameof(SqlServerClient<T>)}. {e.Message}");
            throw;
        }
    }
    public async Task<T> CreateOrRestore(string? sourceId = null)
    {
        try
        {
            logger.LogInformation($"Restoring aggregate {typeof(T).Name} started.");

            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlCommand command = sqlConnection.CreateCommand();

            sourceId ??= await GenerateSourceId(command);
            var aggregate = typeof(T).CreateAggregate<T>(sourceId);

            var sourcedEvents = await LoadEventSource(command, () => new SqlParameter("sourceId", sourceId));
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
        if(aggregate.PendingEvents.Any())
        {
            await using SqlConnection sqlConnection = new(conn);
            await sqlConnection.OpenAsync();
            await using SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
            try
            {
                await using SqlCommand command = sqlConnection.CreateCommand();
                command.Transaction = sqlTransaction;
                PrepareCommand((names, values, count) => values.Select((x, i) => new SqlParameter
                {
                    ParameterName = names.Keys.ElementAt(i) + count,
                    SqlDbType = (SqlDbType)names.Values.ElementAt(i),
                    SqlValue = x
                }).ToArray(), command, aggregate.PendingEvents.ToArray());
                await command.ExecuteNonQueryAsync();
                aggregate.CommitPendingEvents();

                foreach (var type in TProjections(x => x.Mode == ProjectionMode.Consistent))
                {
                    var record = _projection.Project(type, aggregate.EventStream);
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@longSourceId", LongSourceId);
                    command.Parameters.AddWithValue("@guidSourceId", GuidSourceId);
                    var data = JsonSerializer.Serialize(record, type, SerializerOptions);
                    command.Parameters.AddWithValue("@data", data);
                    command.Parameters.AddWithValue("@type", type.Name);
                    command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
                    command.CommandText = ApplyProjectionCommand(type.Name);
                    await command.ExecuteNonQueryAsync();
                }
                await sqlTransaction.CommitAsync();
            }
            catch(SqlException e)
            {
                await sqlTransaction.RollbackAsync();
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
        logger.LogInformation($"Committed {events} pending event(s) for {aggregate.GetType().Name}");
    }
    public async Task<M?> Project<M>(string sourceId)
    {
        await using SqlConnection sqlConnection = new(conn);
        await sqlConnection.OpenAsync();
        await using SqlCommand command = sqlConnection.CreateCommand();

        var events = await LoadEventSource(command, () => new SqlParameter("sourceId", sourceId));
        var model = _projection.Project<M>(events);
        return model;
    }
}