namespace FluentPdf.Kernel;

/// <summary>
/// Base class for aggregate roots: the consistency boundary and the only entry point
/// through which an aggregate is mutated. Records the domain events it raises.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>The domain events raised since the aggregate was loaded or last cleared.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>Records a domain event to be dispatched after the aggregate is persisted.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        _domainEvents.Add(domainEvent);
    }

    /// <summary>Clears the recorded domain events.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
