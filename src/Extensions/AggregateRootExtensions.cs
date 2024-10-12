using System.Reflection;
using EventStorage.AggregateRoot;
using EventStorage.Events;

namespace EventStorage.Extensions;

public static class AggregateRootExtensions
{
    public static void InvokeApply(this IEventSource aggregate, SourcedEvent e)
    {
        var apply = aggregate.GetType().GetApply(e);
        try
        {
            apply.Invoke(aggregate, [e]);
        }
        catch (TargetInvocationException) { throw; }
    }
}