using System.Reflection;
using AsyncHandler.EventSourcing.Events;

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
}