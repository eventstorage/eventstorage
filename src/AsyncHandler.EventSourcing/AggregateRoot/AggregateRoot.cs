
using AsyncHandler.EventSourcing.Events;

namespace AsyncHandler.EventSourcing.AggregateRoot;

public abstract class AggregateRoot(int aggregateId)
{
    private readonly List<SourceEvent> _events = [];
    public int AggregateId => aggregateId;
    public int Version { get; private set; }
    public IEnumerable<SourceEvent> Events => _events;
    public readonly int TenantID;
    protected virtual void RaiseEvent(SourceEvent e) => Apply(e);
    protected virtual void Apply(SourceEvent e) => BumpVersion(events => events.Add(e));
    private void BumpVersion(Action<List<SourceEvent>> add)
    {
        add(_events);
        Version++;
    }
}