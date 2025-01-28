using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Events;
using EventStorage.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventStorage.Projections;

public class ProjectionRestorer(IServiceProvider sp) : IProjectionRestorer
{
    private readonly ILogger logger = TLogger.Create<ProjectionRestorer>();
    public object? Project(IProjection projection, IEnumerable<SourcedEvent> events, Type model) =>
        Project(events, projection, model);
    public M? Project<M>(IEnumerable<SourcedEvent> events) =>
        (M?) Project(events, sp.GetRequiredService<IProjection<M>>(), typeof(M));
    public bool Subscribes(IEnumerable<SourcedEvent> events, IProjection projection) =>
        projection.GetType().GetMethods().Where(m => m.Name == "Project").Subscribes(events);
    private object? Project(IEnumerable<SourcedEvent> events, object projection, Type model)
    {
        if(!events.Any())
            return null;
        var first = events.First().GetType();
        var initMethod = projection.GetType().GetMethod("Project", [first])??
        throw new Exception($"No suitable projection method found to init {model.Name} with {first.Name}.");
        var record = initMethod.Invoke(projection, [events.First()]);
        // just for debugging
        // if(projection.GetType().Name == "OrderProjection")
            // throw new Exception("no such a htings does exist.");
        foreach (var e in events.ToArray()[1..^0])
        {
            var project = projection.GetType().GetMethod("Project", [e.GetType(), model]);
            if (project == null)
            {
                logger.LogWarning($"No suitable projection method found {model.Name}, {e.GetType().Name}.");
                continue;
            }
            record = project.Invoke(projection, [e, record]);
        }
        return record;
    }
}
