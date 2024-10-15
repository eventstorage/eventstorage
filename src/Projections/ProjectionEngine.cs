using System.Reflection;
using EventStorage.Events;

namespace EventStorage.Projections;

public class ProjectionEngine(IServiceProvider sp) : IProjectionEngine
{
    public M Project<M>(IEnumerable<SourcedEvent> events) where M : class
    {
        var interfaceType = typeof(IProjection<>).MakeGenericType(typeof(M));
        var service = sp.GetService(interfaceType);

        var model = Activator.CreateInstance<M>();
        foreach (var e in events)
        {
            MethodInfo? project = service?.GetType().GetMethod("Project", [typeof(M), e.GetType()]);
            if(project == null)
                continue;
            model = (M?) project.Invoke(service, [model, e]);
        }
        return model?? default!;
    }
}
