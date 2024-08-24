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
}