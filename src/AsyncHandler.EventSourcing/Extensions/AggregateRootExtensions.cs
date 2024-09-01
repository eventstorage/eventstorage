using System.Reflection;
using AsyncHandler.EventSourcing.Events;

namespace AsyncHandler.EventSourcing.Extensions;

public static class AggregateRootExtensions
{
    public static void InvokeApply(this AggregateRoot aggregate,  SourceEvent e)
    {
        var apply = aggregate.GetType().GetApply(e);
        try
        {
            apply.Invoke(aggregate, [e]);
        }
        catch(TargetInvocationException){ throw; }
    }
}