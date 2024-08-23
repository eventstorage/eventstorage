using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing;

public class EventSourceConfiguration : AsyncHandlerConfiguration
{
    public EventSourceConfiguration(IServiceCollection services) : base(services)
    {
        
    }
}