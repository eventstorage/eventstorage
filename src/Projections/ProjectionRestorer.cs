using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventStorage.Projections;

public class ProjectionRestorer(
    IServiceProvider sp, Dictionary<IProjection, List<MethodInfo>> projections) : IProjectionRestorer
{
    private readonly ILogger logger = new LoggerFactory().CreateLogger<ProjectionRestorer>();
    public object? Project(IProjection projection, IEnumerable<SourcedEvent> events, Type model) =>
        Project(events, projection, model);
    public object? ProjectOptimized(IProjection projection, IEnumerable<SourcedEvent> events, Type model) =>
        ProjectPreCompiled(events, projection, model);
    public M? Project<M>(IEnumerable<SourcedEvent> events) =>
        (M?) Project(events, sp.GetRequiredService<IProjection<M>>(), typeof(M));
    public M? ProjectOptimized<M>(IEnumerable<SourcedEvent> events) =>
        (M?) ProjectPreCompiled(events, sp.GetRequiredService<IProjection<M>>(), typeof(M));
    public bool Subscribes(IEnumerable<SourcedEvent> events, IProjection projection) =>
        projection.GetType().GetMethods().Where(m => m.Name == "Project")
        .Any(m => events.Any(e =>  e.GetType().IsAssignableFrom(m.GetParameters()
        .FirstOrDefault(x => typeof(SourcedEvent).IsAssignableFrom(x.ParameterType))?.ParameterType)
        ));
    private object? Project(IEnumerable<SourcedEvent> events, object projection, Type model)
    {
        try
        {
            var first = events.First().GetType();
            var initMethod = projection.GetType().GetMethod("Project", [first])??
            throw new Exception($"No suitable projection method found to init {model.Name} with {first.Name}.");
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
            return record;
        }
        catch (TargetInvocationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Failure restoring {model.Name}.{Environment.NewLine}{e.Message}");
            throw;
        }
    }
    private object? ProjectPreCompiled(IEnumerable<SourcedEvent> events, object projection, Type model)
    {
        try
        {
            var methods = projections[(IProjection)projection];
            var record = methods.First().Invoke(projection, [events.First()]);
            foreach (var e in events.Skip(1))
            {
                var method = methods.FirstOrDefault(m => 
                    m.GetParameters().First().ParameterType.IsAssignableFrom(record?.GetType()) &&
                    m.GetParameters().Last().ParameterType.IsAssignableFrom(e.GetType()));
                if (method == null)
                    continue;
                record = method.Invoke(projection, [record, e]);
            }
            return record;
        }
        catch (TargetInvocationException e)
        {
            if(logger.IsEnabled(LogLevel.Error))
                logger.LogError($"Projection failure for {model.Name}. {e.Message}");
            throw;
        }
    }
}
