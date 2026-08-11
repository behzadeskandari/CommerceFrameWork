using Commerce.Framework.Core.Entities;
using Commerce.Framework.Core.Events;
using Xunit;

namespace Commerce.Tests.Unit.Core;

public sealed class DomainEventTests
{
    private sealed record TestEvent : DomainEvent;

    private sealed class TestAggregate : AggregateRoot
    {
        public void DoSomething() => RaiseDomainEvent(new TestEvent());
    }

    [Fact]
    public void AggregateRoot_AddsDomainEvent()
    {
        var aggregate = new TestAggregate();

        aggregate.DoSomething();

        Assert.Single(aggregate.GetDomainEvents());
        Assert.IsType<TestEvent>(aggregate.GetDomainEvents().First());
    }

    [Fact]
    public void AggregateRoot_ClearsDomainEvents()
    {
        var aggregate = new TestAggregate();
        aggregate.DoSomething();

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.GetDomainEvents());
    }

    [Fact]
    public void Entity_WithSameId_AreEqual()
    {
        var first = new TestEntity(10);
        var second = new TestEntity(10);

        Assert.Equal(first, second);
    }

    private sealed class TestEntity : Entity
    {
        public TestEntity(int id) => Id = id;
    }
}
