using CoreApp.Contracts;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure.Popups;

namespace UnitTests;

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
    public void SetTotalQuantity_UsesAssignedQuantityAsMinimum()
    {
        var definition = service.SetTotalQuantity(initialValue: 7, assignedQuantity: 5);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Title, Is.EqualTo("Set total quantity"));
            Assert.That(definition.Min, Is.EqualTo(5));
            Assert.That(definition.InitialValue, Is.EqualTo(7));
        });
    }

    [Test]
    public void SetTotalQuantity_WhenNothingAssigned_RequiresAtLeastOne()
    {
        var definition = service.SetTotalQuantity(initialValue: 1, assignedQuantity: 0);

        Assert.That(definition.Min, Is.EqualTo(1));
    }
}
