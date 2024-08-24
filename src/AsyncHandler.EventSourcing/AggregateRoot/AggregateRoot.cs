
using System.Reflection;
using AsyncHandler.EventSourcing.Events;
using AsyncHandler.EventSourcing.Extensions;

namespace AsyncHandler.EventSourcing.AggregateRoot;

public abstract class AggregateRoot(string aggregateId)
{
    private readonly List<SourceEvent> _events = [];
    public string AggregateId => aggregateId;
    public int Version { get; private set; }
    public IEnumerable<SourceEvent> Events => _events;
    public string? TenantId = string.Empty;
    protected virtual void Apply(SourceEvent e) => BumpVersion(events => events.Add(e));
    private void BumpVersion(Action<List<SourceEvent>> add)
    {
        add(_events);
        Version++;
    }
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
}