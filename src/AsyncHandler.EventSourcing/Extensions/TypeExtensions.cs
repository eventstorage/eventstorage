using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using AsyncHandler.EventSourcing.Events;
using Microsoft.Data.SqlClient;

namespace AsyncHandler.EventSourcing.Extensions;

public static class TypeExtensions
{
    public static MethodInfo GetApply(this Type type, SourceEvent e) =>
        type.GetMethods().FirstOrDefault(m => m.Equals("Apply") &&
        m.Attributes == MethodAttributes.Private &&
        m.GetParameters().First().ParameterType == e.GetType())
        ?? throw new Exception($"No handler defined for the {e.GetType()} event.");

    
    public static void InvokeApply(this Type type, SourceEvent e)
    {
        var apply = type.GetApply(e);
        try
        {
            apply.Invoke(type, [e]);
        }
        catch(TargetInvocationException){ throw; }
    }
    public static T CreateAggregate<T>(this Type type, string aggregateId)
    {
        var constructor = type.GetConstructor([typeof(AggregateRoot)]);
        try
        {
            var aggregate = constructor?.Invoke(type, [aggregateId]) ??
                throw new Exception($"Provided type {typeof(T)} is not an aggregate.");
            return (T) aggregate;
        }
        catch(TargetInvocationException) { throw; }
        catch(Exception) { throw; }
    }
    public static void CreateIfNotExists(this SqlConnection sqlConnection, string str)
    {
        
    }
    public static Type GetClientAggregate(this Type type, Assembly assembly)
    {
        var aggregate = assembly.GetTypes().FirstOrDefault(x => typeof(AggregateRoot).IsAssignableFrom(x));
        if(aggregate != null)
            return aggregate;

        var referencedAssemblies = assembly.GetReferencedAssemblies()
        .Where(x => x.Name != Assembly.GetAssembly(typeof(AggregateRoot))?.GetName()?.Name);
        foreach (var assemblyName in referencedAssemblies)
        {
            aggregate = Assembly.Load(assemblyName)
            .GetTypes().FirstOrDefault(t => typeof(AggregateRoot).IsAssignableFrom(t));
        }
        return aggregate ?? throw new Exception("No aggregate defined.");
    }
}