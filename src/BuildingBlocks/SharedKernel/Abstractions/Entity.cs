namespace SharedKernel.Abstractions;

/// Base class for all domain entities.
/// Identity and equality are determined solely by Id, not by field values.
public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Entity Id must not be an empty Guid.", nameof(id));

        Id = id;
    }

    /// Required by EF Core (parameterless constructor for materialization).
    /// Must not be used directly in domain code.
    protected Entity() { }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        var other = (Entity)obj;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}