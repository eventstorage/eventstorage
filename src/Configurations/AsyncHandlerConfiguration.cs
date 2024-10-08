using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Configurations;

public class AsyncHandlerConfiguration(IServiceCollection services, string? schema = null)
{
    internal IServiceCollection ServiceCollection = services;
    public string Schema = schema ?? "ah";
}