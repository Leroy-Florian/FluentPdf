namespace FluentPdf.Kernel;

/// <summary>
/// Base class for value objects: immutable types compared by the values of their
/// components rather than by reference.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>The ordered components that define equality for this value object.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;

            foreach (var component in GetEqualityComponents())
            {
                hash = (hash * 23) + (component?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
