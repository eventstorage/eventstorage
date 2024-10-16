using System.Reflection;
using EventStorage.Events;
using Microsoft.Extensions.DependencyInjection;

namespace EventStorage.Projections;

public class ProjectionEngine(IServiceProvider sp) : IProjectionEngine
{
    public M Project<M>(IEnumerable<SourcedEvent> events)
    {
        var projection = sp.GetRequiredService<IProjection<M>>();
        var model = projection.Init(events.First());
        foreach (var e in events)
        {
            var project = projection.GetType().GetMethod("Project", [typeof(M), e.GetType()]);
            if(project == null)
                continue;
            project.Invoke(projection, [model, e]);
        }
        return model;
    }
}
