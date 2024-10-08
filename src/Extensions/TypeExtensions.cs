using System.Reflection;
using EventStorage.Events;

namespace EventStorage.Extensions;

public static class TypeExtensions
{
    public static MethodInfo GetApply(this Type type, SourcedEvent e) =>
        type.GetMethods().FirstOrDefault(m => m.Name.Equals("Apply") &&
        m.GetParameters().First().ParameterType.IsAssignableFrom(e.GetType()))
        ?? throw new Exception($"No handler defined for the {e.GetType().Name} event.");
    public static T CreateAggregate<T>(this Type type, string sourceId)
    {
        var cons = type.GetConstructor([])??
            throw new ArgumentException($"{typeof(T).Name} is not a valid aggregate. "+
            "no parameterless constructor found.");
        dynamic aggregate = cons.Invoke([]);
        if(long.TryParse(sourceId, out long longId))
            aggregate.SourceId = longId;
        else if(Guid.TryParse(sourceId, out Guid guidId))
            aggregate.SourceId = guidId;
        else
            throw new ArgumentException($"Invalid sourceId {sourceId}.");
        return (T) aggregate;
    }
}