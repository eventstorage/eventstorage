using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using AsyncHandler.EventSourcing.Events;
using Microsoft.Data.SqlClient;

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
    public static Type? GetAggregate(this Type type, Assembly caller)
    {
        var aggregate = caller.GetTypes()
        .FirstOrDefault(x => typeof(AggregateRoot).IsAssignableFrom(x));
        if(aggregate != null)
            return aggregate;
        
        var refs = caller.GetReferencedAssemblies()
        .Where(r => !r.FullName.StartsWith("Microsoft") && !r.FullName.StartsWith("System"));

        return refs.Where(x => Assembly.Load(x).GetReferencedAssemblies()
        .Any(r => AssemblyName.ReferenceMatchesDefinition(r, typeof(AggregateRoot).Assembly.GetName())))
        .SelectMany(x => Assembly.Load(x).GetTypes())
        .FirstOrDefault(t => typeof(AggregateRoot).IsAssignableFrom(t));
    }
}