using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;

namespace AsyncHandler.EventSourcing.AggregateRoot;

public abstract class AggregateRoot(int aggregateId)
{
    private readonly List<EventSource> _events = [];
    public int AggregateId => aggregateId;
    public int Version { get; private set; }
    public IEnumerable<EventSource> Events => _events;
    public readonly int TenantID; 
    protected virtual void RaiseEvent(Func<AggregateRoot, EventSource> func) => _events.Add(func(this));
    protected virtual void BumpVersion() => Version++;
}