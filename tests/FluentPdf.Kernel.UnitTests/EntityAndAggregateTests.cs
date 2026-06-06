using FluentPdf.Kernel;

namespace FluentPdf.Kernel.UnitTests;

public sealed class EntityAndAggregateTests
{
    [Fact]
    public void Entities_with_the_same_id_are_equal()
    {
        var id = Guid.NewGuid();
        var a = new Customer(id);
        var b = new Customer(id);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var a = new Customer(Guid.NewGuid());
        var b = new Customer(Guid.NewGuid());

        a.Equals(b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void An_entity_is_not_equal_to_null()
    {
        var a = new Customer(Guid.NewGuid());

        a.Equals(null).Should().BeFalse();
        (a == null).Should().BeFalse();
    }

    [Fact]
    public void Constructing_an_entity_with_a_null_id_throws()
    {
        var act = () => new Reference(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Raising_a_domain_event_records_it()
    {
        var order = new Order(Guid.NewGuid());

        order.Place();

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlaced>();
    }

    [Fact]
    public void Clearing_domain_events_empties_the_collection()
    {
        var order = new Order(Guid.NewGuid());
        order.Place();

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Raising_a_null_domain_event_throws()
    {
        var order = new Order(Guid.NewGuid());

        var act = order.RaiseNull;

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class Customer(Guid id) : Entity<Guid>(id);

    private sealed class Reference(string id) : Entity<string>(id);

    private sealed record OrderPlaced : IDomainEvent;

    private sealed class Order(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Place() => RaiseDomainEvent(new OrderPlaced());

        public void RaiseNull() => RaiseDomainEvent(null!);
    }
}
