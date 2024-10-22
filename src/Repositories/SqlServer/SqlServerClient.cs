using System.Data;
using System.Runtime.Serialization;
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
    public async Task Init()
    {
        try
        {
            logger.LogInformation($"Begin initializing {nameof(SqlServerClient<T>)}.");
            _semaphore.Wait();
            using SqlConnection sqlConnection = new(conn);
            sqlConnection.Open();
            using SqlCommand command = new(CreateSchemaIfNotExists, sqlConnection);
            await command.ExecuteNonQueryAsync();
            foreach (var item in ProjectionTypes)
            {
                command.CommandText = CreateProjectionIfNotExists(item?.Name?? "");
                await command.ExecuteNonQueryAsync();
            }
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
        try
        {
            if(aggregate.PendingEvents.Any())
            {
                await using SqlConnection sqlConnection = new(conn);
                await sqlConnection.OpenAsync();
                await using SqlCommand command = sqlConnection.CreateCommand();
                PrepareCommand((names, values, count) => values.Select((x, i) => new SqlParameter
                {
                    ParameterName = names.Keys.ElementAt(i) + count,
                    SqlDbType = (SqlDbType)names.Values.ElementAt(i),
                    SqlValue = x
                }).ToArray(), command, aggregate.PendingEvents.ToArray());
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
    public async Task<M> Project<M>(string sourceId)
    {
        await using SqlConnection sqlConnection = new(conn);
        await sqlConnection.OpenAsync();
        await using SqlCommand command = sqlConnection.CreateCommand();

        var projection = Sp.GetRequiredService<IProjectionEngine>();
        var events = await LoadEventSource(command, () => new SqlParameter("sourceId", sourceId));
        var model = projection.Project<M>(events);
        // var projections = Projections;
        return model;
    }
}