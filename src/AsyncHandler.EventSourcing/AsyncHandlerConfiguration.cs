using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing;

public class AsyncHandlerConfiguration(IServiceCollection services)
{
    internal IServiceCollection ServiceCollection = services;
}