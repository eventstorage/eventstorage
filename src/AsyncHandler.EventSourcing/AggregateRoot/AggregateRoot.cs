using AsyncHandler.EventSourcing.Events;

namespace AsyncHandler.EventSourcing;

public abstract class AggregateRoot(long sourceId)
{
    public long SourceId => sourceId;
    public long Version { get; private set; }
    private readonly List<SourceEvent> _pendingEvents = [];
    private readonly List<SourceEvent> _eventStream = [];
    public IEnumerable<SourceEvent> EventStream => _eventStream;
    public IEnumerable<SourceEvent> PendingEvents => _pendingEvents;
    public string? TenantId;
    public string? CorrelationId;
    protected abstract void Apply(SourceEvent e);
    private void BumpVersion(Action bump) => bump();
    protected virtual void RaiseEvent(SourceEvent e)
    {
        e = e with
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Version = Version + 1,
            SourceId = sourceId,
            SourceType = GetType().Name,
        };
        TenantId = e.TenantId ?? TenantId;
        CorrelationId = e.CorrelationId ?? CorrelationId;
        Apply(e);
        BumpVersion(() => { _pendingEvents.Add(e); Version++; });
    }
    public void RestoreAggregate(IEnumerable<SourceEvent> events)
    {
        foreach (var e in events)
        {
            Apply(e);
            BumpVersion(delegate { _eventStream.Add(e); Version++; });
            TenantId = e.TenantId ?? TenantId;
            CorrelationId = e.CorrelationId ?? CorrelationId;
        }
    }
    public void CommitPendingEvents()
    {
        _eventStream.AddRange(_pendingEvents);
        _pendingEvents.Clear();
    }
}