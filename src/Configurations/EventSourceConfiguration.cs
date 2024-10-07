using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing.Configuration;

public class EventSourceConfiguration(IServiceCollection services, string schema) 
    : AsyncHandlerConfiguration(services, schema)
{

}