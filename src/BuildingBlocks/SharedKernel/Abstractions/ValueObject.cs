namespace SharedKernel.Abstractions;

/// Base class for all value objects.
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// Returns all values that participate in equality comparison.
    /// Subclasses must implement this to enumerate their meaningful fields.
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.GetType() != GetType()) return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(
                seed: default(HashCode),
                func: (hashCode, component) =>
                {
                    hashCode.Add(component);
                    return hashCode;
                })
            .ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}