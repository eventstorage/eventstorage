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
    public string TenantId = string.Empty;
    private string? _causationId;
    protected abstract void Apply(SourceEvent e);
    private void BumpVersion(Action bump) => bump();
    protected virtual void RaiseEvent(SourceEvent e)
    {
        e = e with
        {
            Id = e.Id ?? Guid.NewGuid().ToString(),
            Version = Version + 1,
            Type = e.GetType().Name,
            SourceId = sourceId,
            SourceType = GetType().Name,
            Timestamp = DateTime.UtcNow,
            CausationId = _causationId
        };
        RestoreAggregate(Restoration.Pending, e);
    }
    public void RestoreAggregate(Restoration type, params IEnumerable<SourceEvent> events)
    {
        foreach (var e in events)
        {
            Apply(e);
            Action bump = type switch {
                Restoration.Pending => delegate {_pendingEvents.Add(e); Version++; },
                Restoration.Stream => delegate {_eventStream.Add(e); Version++; },
                _ => () => {}
            };
            BumpVersion(bump);
            TenantId = e.TenantId ?? TenantId;
            _causationId = e.Id;
        }
    }
    public void CommitPendingEvents()
    {
        _eventStream.AddRange(_pendingEvents);
        _pendingEvents.Clear();
    }
}