using EventStorage.Events;

namespace EventStorage.AggregateRoot;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot where TId : IComparable
{
    private readonly List<SourcedEvent> _pendingEvents = [];
    private readonly List<SourcedEvent> _eventStream = [];
    public long Version { get; private set; }
    public IEnumerable<SourcedEvent> EventStream => _eventStream;
    public IEnumerable<SourcedEvent> PendingEvents => _pendingEvents;
    private string? _tenantId;
    private string? _causationId;
    public string? TenantId => _tenantId;
    protected abstract void Apply(SourcedEvent e);
    private void BumpVersion(Action append) { append(); Version++; }
    protected virtual void RaiseEvent(SourcedEvent e)
    {
        e = e with
        {
            Id = Guid.NewGuid(),
            Version = Version + 1,
            Type = e.GetType().Name,
            SourceId = SourceId,
            SourceType = GetType().Name,
            Timestamp = DateTime.UtcNow,
            CausationId = _causationId,
        };
        RestoreAggregate(RestoreType.Pending, e);
    }
    public void RestoreAggregate(RestoreType type, params SourcedEvent[] events)
    {
        foreach (var e in events)
        {
            Apply(e);
            BumpVersion(type switch
            {
                RestoreType.Pending => delegate { _pendingEvents.Add(e); }
                ,
                _ => () => _eventStream.Add(e)
            });
            _causationId = e.Id.ToString();
            _tenantId = e.TenantId;
        }
    }
    public void CommitPendingEvents()
    {
        _eventStream.AddRange(_pendingEvents);
        _pendingEvents.Clear();
    }
}