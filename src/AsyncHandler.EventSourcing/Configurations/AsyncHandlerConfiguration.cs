using Microsoft.Extensions.DependencyInjection;

namespace AsyncHandler.EventSourcing.Configuration;

public class AsyncHandlerConfiguration(IServiceCollection services)
{
    internal IServiceCollection ServiceCollection = services;
}