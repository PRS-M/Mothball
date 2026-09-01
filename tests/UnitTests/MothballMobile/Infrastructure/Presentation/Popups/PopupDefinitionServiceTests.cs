using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.Shared;
using MothballMobile.Infrastructure.Presentation.Popups;

namespace Mothball.Tests.Unit.Mobile.Infrastructure.Presentation.Popups;

public class PopupDefinitionServiceTests
{
    private readonly PopupDefinitionService service = new();

    [Test]
    public void RestorePolicyPicker_MapsLabelsToPolicies()
    {
        var definition = service.RestorePolicyPicker();

        Assert.That(definition.Title, Is.EqualTo("Restore mode"));
        Assert.That(definition.Options.Select(option => option.Value), Is.EqualTo(new[]
        {
            InventoryBackupConflictPolicy.AddOnly,
            InventoryBackupConflictPolicy.AddAndUpsertMetadata,
            InventoryBackupConflictPolicy.FullSync,
            InventoryBackupConflictPolicy.StrictFullSync,
        }));
    }

    [Test]
    public void PhotoSourcePicker_MapsLabelsToSources()
    {
        var definition = service.PhotoSourcePicker();

        Assert.That(definition.Options.Select(option => option.Value), Is.EqualTo(new[]
        {
            PhotoSource.Library,
            PhotoSource.Camera,
        }));
    }

    [Test]
    public void RemoveItemFromContainer_FormatsItemNameInCentralDefinition()
    {
        var definition = service.RemoveItemFromContainer("Drill");

        Assert.That(definition.Title, Is.EqualTo("Remove item"));
        Assert.That(definition.Message, Is.EqualTo("Remove 'Drill' from this container? The item itself will not be deleted."));
        Assert.That(definition.Accept, Is.EqualTo("Remove"));
        Assert.That(definition.Cancel, Is.EqualTo("Cancel"));
    }

    [Test]
    public void ItemPhotoDeletePicker_BuildsStablePhotoOptions()
    {
        var first = new ImageItem(Guid.NewGuid());
        var second = new ImageItem(Guid.NewGuid());

        var definition = service.ItemPhotoDeletePicker(new[] { first, second });

        Assert.That(definition.Options.Select(option => option.Label), Is.EqualTo(new[] { "Photo 1", "Photo 2" }));
        Assert.That(definition.Options.Select(option => option.Value), Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void SetQuantity_ProvidesModalTextAndValidationMessages()
    {
        var definition = service.SetQuantity(7);

        Assert.That(definition.Title, Is.EqualTo("Set quantity"));
        Assert.That(definition.InitialValue, Is.EqualTo(7));
        Assert.That(definition.InvalidNumberMessage, Is.EqualTo("Enter a number between 0 and 1000."));
        Assert.That(definition.OutOfRangeMessage, Is.EqualTo("Value must be between 0 and 1000."));
    }

    [Test]
    public void SetTotalQuantity_AllowsZeroToEnterDeletionWorkflow()
    {
        var definition = service.SetTotalQuantity(initialValue: 7, assignedQuantity: 5);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Title, Is.EqualTo("Set total quantity"));
            Assert.That(definition.Min, Is.Zero);
            Assert.That(definition.InitialValue, Is.EqualTo(7));
        });
    }

    [Test]
    public void SetTotalQuantity_WhenNothingAssigned_StillAllowsDeletionWorkflow()
    {
        var definition = service.SetTotalQuantity(initialValue: 1, assignedQuantity: 0);

        Assert.That(definition.Min, Is.Zero);
    }

    [Test]
    public void AssociateUnassignedQuantity_UsesRemainingQuantityAsMaximum()
    {
        var definition = service.AssociateUnassignedQuantity(6);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Title, Is.EqualTo("Assign to container"));
            Assert.That(definition.Min, Is.EqualTo(1));
            Assert.That(definition.Max, Is.EqualTo(6));
            Assert.That(definition.InitialValue, Is.EqualTo(6));
            Assert.That(definition.Message, Does.Contain("unassigned items"));
        });
    }

    [Test]
    public void WithdrawalContainerPicker_ListsOnlyProvidedRemainingAllocations()
    {
        var allocation = new ItemContainerAllocation(Guid.NewGuid(), "Box", 3);

        var definition = service.WithdrawalContainerPicker([allocation]);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Options, Has.Count.EqualTo(1));
            Assert.That(definition.Options[0].Label, Is.EqualTo("Box (3)"));
            Assert.That(definition.Options[0].Value, Is.EqualTo(allocation));
        });
    }

    [Test]
    public void WithdrawFromContainer_DefaultsToRemainingRequiredQuantity()
    {
        var allocation = new ItemContainerAllocation(Guid.NewGuid(), "Box", 10);

        var definition = service.WithdrawFromContainer(
            allocation,
            carriedQuantity: 0,
            requiredQuantity: 4);

        Assert.That(definition.InitialValue, Is.EqualTo(4));
        Assert.That(definition.Message, Does.Contain("withdraw from this container"));
    }

    [Test]
    public void WithdrawFromContainer_WhenCarryIsHigher_DefaultsToCarry()
    {
        var allocation = new ItemContainerAllocation(Guid.NewGuid(), "Box", 3);

        var definition = service.WithdrawFromContainer(
            allocation,
            carriedQuantity: 5,
            requiredQuantity: 2);

        Assert.That(definition.InitialValue, Is.EqualTo(5));
        Assert.That(definition.Message, Does.Contain("withdraw from this container"));
    }

    [Test]
    public void ConfirmUnassignedWithdrawal_ExplainsThatTotalWillDecrease()
    {
        var definition = service.ConfirmUnassignedWithdrawal(4);

        Assert.That(definition.Message, Does.Contain("unassigned"));
        Assert.That(definition.Message, Does.Contain("reduce the total quantity"));
    }

    [Test]
    public void ConsumptionSourcePicker_ListsContainersAndUnassignedStock()
    {
        var item = new CoreApp.Domain.Entities.ItemAggregate.Item(Guid.NewGuid(), "Widget", "");
        var allocation = new ItemContainerAllocation(Guid.NewGuid(), "Box", 3);
        var inventory = new InventorySnapshot(item, 5, 3, [allocation]);

        var definition = service.ConsumptionSourcePicker(inventory);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Options.Select(option => option.Label),
                Is.EqualTo(new[] { "Box (3)", "Unassigned stock (2)" }));
            Assert.That(definition.Options[0].Value.ContainerId, Is.EqualTo(allocation.ContainerId));
            Assert.That(definition.Options[1].Value.Kind,
                Is.EqualTo(ItemInventoryConsumptionSourceKind.Unassigned));
        });
    }

    [Test]
    public void ConsumeFromContainer_CapsQuantityAtSelectedSource()
    {
        var definition = service.ConsumeFromContainer(
            new ItemContainerAllocation(Guid.NewGuid(), "Box", 4));

        Assert.Multiple(() =>
        {
            Assert.That(definition.Min, Is.EqualTo(1));
            Assert.That(definition.Max, Is.EqualTo(4));
            Assert.That(definition.InitialValue, Is.EqualTo(1));
        });
    }

    [Test]
    public void DeleteItemBySettingTotalToZero_ExplainsPermanentRemoval()
    {
        var definition = service.DeleteItemBySettingTotalToZero("Widget");

        Assert.Multiple(() =>
        {
            Assert.That(definition.Message, Does.Contain("Widget"));
            Assert.That(definition.Message, Does.Contain("permanently remove"));
            Assert.That(definition.Message, Does.Contain("photos"));
        });
    }
}
