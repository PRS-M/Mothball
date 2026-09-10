using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.Events;

namespace Mothball.Tests.Unit.Core.DomainEvents;

[TestFixture]
public sealed class DomainEventTests
{
    [Test]
    public void Item_Creation_RecordsCreationEvent()
    {
        var item = new Item(Guid.NewGuid(), "Cable", string.Empty);

        var created = item.DomainEvents.OfType<ItemCreated>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(created.ItemId, Is.EqualTo(item.ItemId));
            Assert.That(created.Name, Is.EqualTo("Cable"));
        });
    }

    [Test]
    public void Item_UpdateDetails_WhenValuesAreUnchanged_DoesNotRecordEvent()
    {
        var item = new Item(Guid.NewGuid(), "Cable", "USB-C");
        item.ClearDomainEvents();

        item.UpdateDetails("Cable", "USB-C");

        Assert.That(item.DomainEvents, Is.Empty);
    }

    [Test]
    public void Inventory_AllocationChange_RecordsOneInventoryChangedEvent()
    {
        var inventory = new ItemInventory(Guid.NewGuid(), 5);
        inventory.SetContainerAllocation(Guid.NewGuid(), "Box", 2);

        var changed = inventory.DomainEvents.OfType<InventoryChanged>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(changed.PreviousAssignedQuantity, Is.EqualTo(0));
            Assert.That(changed.AssignedQuantity, Is.EqualTo(2));
            Assert.That(changed.AffectedContainerIds, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Inventory_HydrationConstructor_DoesNotRecordEvents()
    {
        var inventory = new ItemInventory(
            Guid.NewGuid(),
            5,
            [new ItemContainerAllocation(Guid.NewGuid(), "Box", 2)]);

        Assert.That(inventory.DomainEvents, Is.Empty);
    }

    [Test]
    public void Aggregate_ClearDomainEvents_RemovesPendingEvents()
    {
        var item = new Item(Guid.NewGuid(), "Cable", string.Empty);

        item.ClearDomainEvents();

        Assert.That(item.DomainEvents, Is.Empty);
    }

    [Test]
    public void Item_PhotoLifecycle_RecordsAddedAndRemovedEvents()
    {
        var item = new Item(Guid.NewGuid(), "Cable", string.Empty);
        item.ClearDomainEvents();

        var photo = item.AddImageItem();
        item.RemoveImageItem(photo.ImageId);

        Assert.That(
            item.DomainEvents.Select(domainEvent => domainEvent.GetType()).ToArray(),
            Is.EqualTo(new[] { typeof(ItemPhotoAdded), typeof(ItemPhotoRemoved) }));
    }
}
