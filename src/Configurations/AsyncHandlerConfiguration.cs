using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing.Configuration;

public class AsyncHandlerConfiguration(IServiceCollection services, string? schema = null)
{
    internal IServiceCollection ServiceCollection = services;
    public string Schema = schema?? "ah";
}