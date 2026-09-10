using System.Text.Json;
using CoreApp.Application.Abstractions.DomainEvents;
using CoreApp.Application.Abstractions.Sync;
using CoreApp.Domain.Abstractions;
using CoreApp.Domain.Events;

namespace CoreApp.Application.Features.Sync;

/// <summary>Converts committed domain events into durable synchronization messages.</summary>
public sealed class SyncOutboxEventHandler : IDomainEventHandler
{
    private const int CurrentSchemaVersion = 1;
    private readonly ISyncOutboxStore outbox;
    private readonly ISyncDeviceIdentity deviceIdentity;

    public SyncOutboxEventHandler(ISyncOutboxStore outbox, ISyncDeviceIdentity deviceIdentity)
    {
        this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        this.deviceIdentity = deviceIdentity ?? throw new ArgumentNullException(nameof(deviceIdentity));
    }

    /// <inheritdoc />
    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var aggregate = GetAggregate(domainEvent);
        var message = new SyncOutboxMessage(
            domainEvent.EventId,
            deviceIdentity.DeviceId,
            0,
            domainEvent.GetType().Name,
            CurrentSchemaVersion,
            domainEvent.OccurredUtc,
            aggregate.Type,
            aggregate.Id,
            JsonSerializer.Serialize(domainEvent, domainEvent.GetType()));
        return outbox.EnqueueAsync([message], cancellationToken);
    }

    private static (string Type, Guid? Id) GetAggregate(IDomainEvent domainEvent)
        => domainEvent switch
        {
            ItemCreated value => ("Item", value.ItemId),
            ItemDetailsUpdated value => ("Item", value.ItemId),
            ItemBarcodeChanged value => ("Item", value.ItemId),
            ItemDeleted value => ("Item", value.ItemId),
            ItemPhotoAdded value => ("Item", value.ItemId),
            ItemPhotoRemoved value => ("Item", value.ItemId),
            InventoryChanged value => ("ItemInventory", value.ItemId),
            InventoryWithdrawn value => ("ItemInventory", value.ItemId),
            ItemExhausted value => ("Item", value.ItemId),
            ContainerCreated value => ("Container", value.ContainerId),
            ContainerDetailsUpdated value => ("Container", value.ContainerId),
            ContainerBarcodeChanged value => ("Container", value.ContainerId),
            ContainerDeleted value => ("Container", value.ContainerId),
            ContainerPhotoAdded value => ("Container", value.ContainerId),
            ContainerPhotoRemoved value => ("Container", value.ContainerId),
            TagCreated value => ("Tag", value.TagId),
            TagRenamed value => ("Tag", value.TagId),
            TagAssigned value => ($"{value.TargetKind}TagAssignment", value.TargetId),
            TagUnassigned value => ($"{value.TargetKind}TagAssignment", value.TargetId),
            InventoryRestored or InventoryReset or InventorySeeded => ("Inventory", null),
            _ => ("Unknown", null),
        };
}
