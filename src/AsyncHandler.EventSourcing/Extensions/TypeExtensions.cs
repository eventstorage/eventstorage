using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using AsyncHandler.EventSourcing.Events;
using Microsoft.Data.SqlClient;

namespace AsyncHandler.EventSourcing.Extensions;

public static class TypeExtensions
{
    public static MethodInfo GetApply(this Type type, SourceEvent e)
    {
        var methods = type.GetMethods();
        return methods.FirstOrDefault(m => m.Name.Equals("Apply") &&
        m.GetParameters().First().ParameterType.IsAssignableFrom(e.GetType()))
        ?? throw new Exception($"No handler defined for the {e.GetType()} event.");
    }

    
    public static void InvokeApply(this AggregateRoot aggregate, AggregateRoot aggregate1,  SourceEvent e)
    {
        var apply = aggregate.GetType().GetApply(e);
        try
        {
            apply.Invoke(aggregate, [e]);
        }
        catch(TargetInvocationException){ throw; }
    }
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
    public static Type? GetClientAggregate(this Type type, Assembly caller)
    {
        var aggregate = caller.GetTypes()
        .FirstOrDefault(x => typeof(AggregateRoot).IsAssignableFrom(x));
        if(aggregate != null)
            return aggregate;

        var mustReferenceAssembly = typeof(AggregateRoot).Assembly.GetName();

        var ideals = caller.GetReferencedAssemblies().Where(x => 
        Assembly.Load(x).GetReferencedAssemblies()
        .Any(x => AssemblyName.ReferenceMatchesDefinition(x, mustReferenceAssembly)));

        foreach (var assemblyName in ideals)
        {
            aggregate = Assembly.Load(assemblyName).GetTypes()
            .FirstOrDefault(t => typeof(AggregateRoot).IsAssignableFrom(t));
            if(aggregate != null)
                return aggregate;
        }
        return aggregate;
    }
}