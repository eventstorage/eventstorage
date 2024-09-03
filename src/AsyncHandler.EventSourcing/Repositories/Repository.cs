using AsyncHandler.EventSourcing.Repositories.AzureSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AsyncHandler.EventSourcing.Repositories;

public class Repository<T>(string conn, IServiceProvider sp) : IRepository<T> where T : AggregateRoot
{
    private AzureSqlClient<T>? _azureSqlClient;
    private AzureSqlClient<T>? _postgreSqlClient;
    private AzureSqlClient<T>? _sqlServerClient;
    public AzureSqlClient<T> AzureSqlClient => _azureSqlClient ??=
        new AzureSqlClient<T>(conn, sp.GetRequiredService<ILogger<AzureSqlClient<T>>>());
    public AzureSqlClient<T> PostgreSqlClient => _postgreSqlClient ??=
        new AzureSqlClient<T>(conn, sp.GetRequiredService<ILogger<AzureSqlClient<T>>>());
    public AzureSqlClient<T> SqlServerClient => _sqlServerClient ??=
        new AzureSqlClient<T>(conn, sp.GetRequiredService<ILogger<AzureSqlClient<T>>>());
}