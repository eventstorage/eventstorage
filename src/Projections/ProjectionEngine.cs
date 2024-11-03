using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventStorage.Projections;

public class ProjectionEngine(
    IServiceProvider sp, Dictionary<IProjection, List<MethodInfo>> projections) : IProjectionEngine
{
    private readonly ILogger logger = new LoggerFactory().CreateLogger<ProjectionEngine>();
    public object? Project(Type model, IEnumerable<SourcedEvent> events) =>
        Project(events, sp.GetRequiredService(typeof(IProjection<>).MakeGenericType(model)), model);
    public M? Project<M>(IEnumerable<SourcedEvent> events) =>
        (M?) Project(events, sp.GetRequiredService<IProjection<M>>(), typeof(M));
    // public bool Subscribes(IEnumerable<SourcedEvent> events, object projection) =>
    //     projection.GetType().GetMethods().Where(m => m.Name == "Project").Any(m => 
    //         events.Any(e =>  e.GetType().IsAssignableFrom(m.GetParameters()
    //         .FirstOrDefault(x => typeof(SourcedEvent).IsAssignableFrom(x.ParameterType))?.ParameterType)
    //     ));
    private object? Project(IEnumerable<SourcedEvent> events, object projection, Type model)
    {
        try
        {
            var initMethod = projection.GetType().GetMethod("Project", [events.First().GetType()])??
                throw new Exception($"No suitable projection method found to initialize {model.Name}.");
            var record = initMethod.Invoke(projection, [events.First()]);
            foreach (var e in events.ToArray()[1..^0])
            {
                var project = projection.GetType().GetMethod("Project", [model, e.GetType()]);
                if (project == null)
                {
                    logger.LogInformation($"No suitable projection method found {model.Name}, {e.GetType().Name}.");
                    continue;
                }
                record = project.Invoke(projection, [record, e]);
            }
            return model;
        }
        catch (TargetInvocationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Projection failure for {model.Name}. {e.Message}");
            throw;
        }
    }
}
