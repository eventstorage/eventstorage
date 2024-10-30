using System.Reflection;
using EventStorage.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventStorage.Projections;

public class ProjectionEngine(IServiceProvider sp) : IProjectionEngine
{
    private readonly ILogger logger = sp.GetRequiredService<ILogger<ProjectionEngine>>();
    public object? Project(Type type, IEnumerable<SourcedEvent> events) =>
        Project(events, sp.GetRequiredService(typeof(IProjection<>).MakeGenericType(type)), type);
    public M? Project<M>(IEnumerable<SourcedEvent> events) =>
        (M?) Project(events, sp.GetRequiredService<IProjection<M>>(), typeof(M));
    private object? Project(IEnumerable<SourcedEvent> events, object projection, Type type)
    {
        try
        {
            var initMethod = projection.GetType().GetMethod("Project", [events.First().GetType()])??
            throw new Exception($"No suitable projection method found to initialize {type.Name}.");
            var model = initMethod.Invoke(projection, [events.First()]);
            foreach (var e in events.ToArray()[1..^0])
            {
                var project = projection.GetType().GetMethod("Project", [type, e.GetType()]);
                if (project == null)
                {
                    logger.LogInformation($"No suitable projection method found {type.Name}, {e.GetType().Name}.");
                    continue;
                }
                model = project.Invoke(projection, [model, e]);
            }
            return model;
        }
        catch (TargetInvocationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Projection failure for {type.Name}. {e.Message}");
            throw;
        }
    }
}
