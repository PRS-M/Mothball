using CoreApp.Application.Abstractions.DomainEvents;
using CoreApp.Application.Utilities;
using CoreApp.Domain.Abstractions;
using CoreApp.Domain.Events;

namespace Mothball.Tests.Unit.Core.DomainEvents;

[TestFixture]
public sealed class DomainEventDispatcherTests
{
    [Test]
    public async Task DispatchAsync_InvokesHandlersInEventOrder()
    {
        var received = new List<IDomainEvent>();
        var handler = new RecordingHandler(received);
        var dispatcher = new DomainEventDispatcher([handler]);
        var events = new IDomainEvent[]
        {
            new ItemCreated(Guid.NewGuid(), "First"),
            new ContainerCreated(Guid.NewGuid(), "Second"),
        };

        await dispatcher.DispatchAsync(events);

        Assert.That(received, Is.EqualTo(events));
    }

    [Test]
    public async Task Subscribe_DisposeStopsNotifications()
    {
        var received = 0;
        var dispatcher = new DomainEventDispatcher([]);
        using (dispatcher.Subscribe(_ => received++))
        {
            await dispatcher.DispatchAsync([new ItemCreated(Guid.NewGuid(), "Item")]);
        }

        await dispatcher.DispatchAsync([new ItemCreated(Guid.NewGuid(), "Ignored")]);

        Assert.That(received, Is.EqualTo(1));
    }

    private sealed class RecordingHandler(List<IDomainEvent> received) : IDomainEventHandler
    {
        public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            received.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
