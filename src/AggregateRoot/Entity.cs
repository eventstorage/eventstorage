
namespace AsyncHandler.EventSourcing;

public abstract class Entity<TId> : IEquatable<Entity<TId>> where TId : IComparable
{
    public Entity()
    {
        ConstraintMatches();
    }
    private TId _sourceId = default!;
    private bool _tIdSet;
    public TId SourceId
    {
        get => _sourceId;
        set
        {
            if(_tIdSet)
                return;
            _sourceId = value;
            _tIdSet = true;
        }
    }
    public override bool Equals(object? obj) =>
        obj is Entity<TId> entity && entity.GetType().Equals(GetType());

    // restricting constraint to long and guid only
    private static void ConstraintMatches() =>
        _ = AllowedTypeArgs.Any(t => t.IsAssignableFrom(typeof(TId))) ? "" : 
        throw new ArgumentException($"unsupported type argument {typeof(TId).Name}.");
        
    public bool Equals(Entity<TId>? other) => Equals((object?)other);

    public static bool operator ==(Entity<TId> left, Entity<TId> right) => Equals(left, right);
    public static bool operator !=(Entity<TId> left, Entity<TId> right) => !Equals(left, right);

    public override int GetHashCode() => _sourceId.GetHashCode();
    public static IEnumerable<Type> AllowedTypeArgs => [typeof(long), typeof(Guid)];
}