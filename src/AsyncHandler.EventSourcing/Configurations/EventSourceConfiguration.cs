using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing.Configuration;

public class EventSourceConfiguration(IServiceCollection services) : AsyncHandlerConfiguration(services)
{
    public EventSources EventSource { get; set; }
}