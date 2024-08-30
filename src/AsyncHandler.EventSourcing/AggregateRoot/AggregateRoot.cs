using AsyncHandler.EventSourcing.Events;

namespace AsyncHandler.EventSourcing;

public abstract class AggregateRoot(string sourceId)
{
    public string SourceId => sourceId;
    public int Version { get; private set; }
    private readonly List<SourceEvent> _pendingEvents = [];
    private readonly List<SourceEvent> _eventStream = [];
    public IEnumerable<SourceEvent> EventStream => _eventStream;
    public IEnumerable<SourceEvent> PendingEvents => _pendingEvents;
    public string TenantId = string.Empty;
    public string CorrelationId = string.Empty;
    protected virtual void Apply(SourceEvent e) => BumpVersion(events => { events.Add(e); Version++; });
    private void BumpVersion(Action<List<SourceEvent>> append) => append(_pendingEvents);
    protected virtual void RaiseEvent(SourceEvent e)
    {
        e = e with
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Version = Version + 1,
            SourceId = sourceId,
            SourceType = e.GetType().Assembly.ToString(),
        };
        TenantId = e.TenantId ??"";
        TenantId = e.CorrelationId ??"";
        Apply(e);
    }
    public void RestoreAggregate(IEnumerable<SourceEvent> events)
    {
        foreach (var e in events)
        {
            Apply(e);
            TenantId = e.TenantId ??"";
            CorrelationId = e.CorrelationId ??"";
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