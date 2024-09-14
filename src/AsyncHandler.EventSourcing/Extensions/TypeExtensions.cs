using System.Reflection;
using AsyncHandler.EventSourcing.Events;

namespace AsyncHandler.EventSourcing.Extensions;

public static class TypeExtensions
{
    public static MethodInfo GetApply(this Type type, SourceEvent e) =>
        type.GetMethods().FirstOrDefault(m => m.Name.Equals("Apply") &&
        m.GetParameters().First().ParameterType.IsAssignableFrom(e.GetType()))
        ?? throw new Exception($"No handler defined for the {e.GetType().Name} event.");
    public static T CreateAggregate<T>(this Type type, long sourceId)
    {
        var constructor = type.GetConstructor([typeof(long)]);
        try
        {
            var aggregate = constructor?.Invoke([sourceId]) ??
                throw new Exception($"Provided type {typeof(T)} is not a valid aggregate.");
            return (T) aggregate;
        }
        catch(TargetInvocationException) { throw; }
        catch(Exception) { throw; }
    }
}