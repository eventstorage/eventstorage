using EventStorage.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Configurations;

public class EventSourceConfiguration(IServiceCollection services, string schema) 
    : EventStorageConfiguration(services, schema)
{

}