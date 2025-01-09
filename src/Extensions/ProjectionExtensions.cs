using System.Reflection;
using EventStorage.Events;
using EventStorage.Projections;

namespace EventStorage.Extensions;

public static class ProjectionExtensions
{
    // validates projection methods at compile-time
    public static IEnumerable<MethodInfo> GetMethods(this Projection projection)
    {
        var initMethod = projection.GetType().GetMethods()
            .Where(m => m.Name == "Project" && m.GetParameters().Length == 1)
            .Where(m => m.ReturnType.IsAssignableFrom(projection.GetType().BaseType?.GenericTypeArguments.First()))
            .FirstOrDefault(m => typeof(SourcedEvent).IsAssignableFrom(m.GetParameters().First().ParameterType))??
            throw new Exception($"No suitable projection method found to init {projection.GetType().Name}");

        var methods = projection.GetType().GetMethods()
            .Where(m => m.Name == "Project" && m.GetParameters().Length == 2)
            .Where(m => m.ReturnType.IsAssignableFrom(projection.GetType().BaseType?.GenericTypeArguments.First()))
            .Where(m => m.GetParameters().Last().ParameterType.IsAssignableFrom(projection.GetType().BaseType?.GenericTypeArguments.First()))
            .Where(m => typeof(SourcedEvent).IsAssignableFrom(m.GetParameters().First().ParameterType)).ToList();
        List<MethodInfo> list = [initMethod];
        list.AddRange(methods);
        return list;
    }
    public static bool Subscribes(this IEnumerable<MethodInfo> methods, IEnumerable<SourcedEvent> events) =>
        methods.Any(m => events.Any(e => e.GetType().IsAssignableFrom(m.GetParameters().First().ParameterType)));
}