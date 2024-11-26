using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Configurations;

public class EventStorageConfiguration(IServiceCollection services, string? schema = null)
{
    internal IServiceCollection ServiceCollection = services;
    internal IServiceProvider Sp => ServiceCollection.BuildServiceProvider();
    public string Schema = schema ?? "es";
}