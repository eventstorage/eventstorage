using AsyncHandler.EventSourcing.Events;

namespace AsyncHandler.EventSourcing;

public abstract class AggregateRoot(string aggregateId)
{
    public string AggregateId => aggregateId;
    public int Version { get; private set; }
    public string? TenantId = string.Empty;
    private readonly List<SourceEvent> _pendingEvents = [];
    private readonly List<SourceEvent> _eventStream = [];
    public IEnumerable<SourceEvent> EventStream => _eventStream;
    public IEnumerable<SourceEvent> PendingEvents => _pendingEvents;
    protected virtual void Apply(SourceEvent e) => BumpVersion(events => events.Add(e));
    private void BumpVersion(Action<List<SourceEvent>> append) { append(_pendingEvents); Version++; }
    protected virtual void RaiseEvent(SourceEvent e)
    {
        e = e with
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Version = Version + 1,
            SourceId = aggregateId,
            SourceType = e.GetType().Assembly.ToString(),
        };
        TenantId = e.TenantId;
        Apply(e);
    }
    public void RestoreAggregate(IEnumerable<SourceEvent> events)
    {
        foreach (var e in events)
        {
            Apply(e);
        }
    }
    public IEnumerable<SourceEvent> CommitPendingEvents()
    {
        IEnumerable<SourceEvent> pending = _pendingEvents;
        _eventStream.AddRange(pending);
        _pendingEvents.Clear();
        return pending;
    }
}