using AsyncHandler.EventSourcing;
using AsyncHandler.EventSourcing.Repositories.AzureSql;
using AsyncHandler.EventSourcing.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class Repository<T>(string connectionString, IServiceProvider sp) : IRepository<T> where T : AggregateRoot
{
    public static string GetSourceCommand => @"SELECT * FROM [dbo].[EventSource] WHERE [AggregateId] = @AggregateId";
    public static string InsertSourceCommand => @"INSERT INTO [dbo].[EventSource] VALUES (@timeStamp, @sequenceNumber, @aggregateId, @aggregateType, @version, @eventType, @data, @correlationId, @tenantId)";
    public static string CreateIfNotExists => $"IF NOT EXISTS(SELECT * FROM sys.tables WHERE NAME = 'EventSource') "+
    "CREATE TABLE [dbo].[EventSource]("+
        $"[{EventSourceSchema.Id}] [bigint] IDENTITY(1,1) NOT NULL,"+
        $"[{EventSourceSchema.Timestamp}] [datetime] NOT NULL,"+
        $"[{EventSourceSchema.SequenceNumber}] [int] NOT NULL,"+
        $"[{EventSourceSchema.AggregateId}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.AggregateType}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.Version}] [int] NOT NULL,"+
        $"[{EventSourceSchema.EventType}] [nvarchar](255) NOT NULL,"+
        // data type is changed to json for Azure later
        $"[{EventSourceSchema.Data}] [nvarchar](4000) NOT NULL,"+
        $"[{EventSourceSchema.CorrelationId}] [nvarchar](255) NOT NULL,"+
        $"[{EventSourceSchema.TenantId}] [nvarchar](255) NOT NULL,"+
    ")";
    private IAzureSqlClient<T>? _azureSqlClient;
    private IAzureSqlClient<T>? _postgreSqlClient;
    private IAzureSqlClient<T>? _sqlServerClient;
    public IAzureSqlClient<T> AzureSqlClient => _azureSqlClient ??= new AzureSqlClient<T>(connectionString, sp);
    public IAzureSqlClient<T> SqlServerClient => _postgreSqlClient ??= new AzureSqlClient<T>(connectionString, sp);
    public IAzureSqlClient<T> PostgreSqlClient => _sqlServerClient ??= new AzureSqlClient<T>(connectionString, sp);
}