using System.Reflection;
using AsyncHandler.EventSourcing.Events;

namespace AsyncHandler.EventSourcing.Extensions;

public static class AggregateRootExtensions
{
    public static void InvokeApply(this IAggregateRoot aggregate,  SourcedEvent e)
    {
        var apply = aggregate.GetType().GetApply(e);
        try
        {
            apply.Invoke(aggregate, [e]);
        }
        catch(TargetInvocationException){ throw; }
    }
}