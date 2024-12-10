using System.Reflection;
using EventStorage.Events;
using EventStorage.Extensions;

namespace EventStorage.AggregateRoot;

public abstract class EventSource<TId> : Entity<TId>, IEventSource where TId : IComparable
{
    private List<SourcedEvent> _pendingEvents = [];
    private List<SourcedEvent> _eventStream = [];
    public int Version { get; private set; }
    public IEnumerable<SourcedEvent> EventStream => _eventStream;
    public IEnumerable<SourcedEvent> PendingEvents => _pendingEvents;
    private string? _tenantId;
    private string? _causationId;
    public string? TenantId => _tenantId;
    protected virtual void Apply(SourcedEvent e) => this.InvokeApply(e);
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
        RestoreAggregate(false, e);
        _pendingEvents.Add(e);
    }
    public void RestoreAggregate(bool stream, params SourcedEvent[] events)
    {
        foreach (var e in events)
        {
            Apply(e);
            Version++;
            if(stream)
            _eventStream.Add(e);
            _causationId = e.Id.ToString();
            _tenantId = e.TenantId;
        }
    }
    public IEnumerable<SourcedEvent> FlushPendingEvents()
    {
        _eventStream.AddRange(_pendingEvents);
        var pending = _pendingEvents;
        _pendingEvents = [];
        return pending;
    }
}