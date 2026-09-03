using CoreApp.Domain.Entities.InventoryAggregate;
﻿using CoreApp.Domain.Entities;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.Entities.Shared;
using CoreApp.Application.Utilities;

namespace Mothball.Tests.Unit.Core.Entities;

[TestFixture]
public class CoreAppTests
{
    [Test]
    public void BaseEntity_Id_Default_IsZero()
    {
        var entity = new BaseEntity();
        Assert.That(entity.Id, Is.EqualTo(0));
    }

    [Test]
    public void Item_AddImageItem_AddsPhoto()
    {
        var item = new Item();
        var countBefore = item.Photos.Count;
        item.AddImageItem();
        Assert.That(item.Photos.Count, Is.EqualTo(countBefore + 1));
    }

    [Test]
    public void Item_RemoveImageItem_RemovesPhoto()
    {
        var item = new Item();
        var img = item.AddImageItem();
        item.RemoveImageItem(img.ImageId);
        Assert.That(item.Photos, Does.Not.Contain(img));
    }

    [Test]
    public void Item_DefaultCtor_GeneratesId()
    {
        var item = new Item();

        Assert.That(item.ItemId, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Item_Ctor_WithEmptyGuid_Throws()
    {
        Assert.That(() => new Item(Guid.Empty, "Name", "Description"), Throws.ArgumentException);
    }

    [Test]
    public void Item_Ctor_WithBlankName_Throws()
    {
        Assert.That(() => new Item(Guid.NewGuid(), " ", "Description"), Throws.ArgumentException);
    }

    [Test]
    public void Item_UpdateDetails_ReplacesCoreFields()
    {
        var item = new Item("Old", "Old description");

        item.UpdateDetails("New", "New description");

        Assert.Multiple(() =>
        {
            Assert.That(item.Name, Is.EqualTo("New"));
            Assert.That(item.Description, Is.EqualTo("New description"));
        });
    }

    [Test]
    public void Item_UpdateDetails_WithNullDescription_NormalizesToEmpty()
    {
        var item = new Item("Name", "Description");

        item.UpdateDetails("Name", null!);

        Assert.That(item.Description, Is.Empty);
    }

    [Test]
    public void Item_UpdateDetails_WithBlankName_Throws()
    {
        var item = new Item("Name", "Description");

        Assert.That(() => item.UpdateDetails("", "Description"), Throws.ArgumentException);
    }

    [Test]
    public void Item_UpdateBarcode_ReplacesAndClearsBarcode()
    {
        var item = new Item("Name", "Description");
        var barcode = new Barcode("  1234567890123  ", BarcodeSymbology.Ean13);

        item.UpdateBarcode(barcode);
        item.UpdateBarcode(null);

        Assert.That(barcode.Value, Is.EqualTo("1234567890123"));
        Assert.That(item.Barcode, Is.Null);
    }

    [Test]
    public void Barcode_WithBlankValue_Throws()
    {
        Assert.That(
            () => new Barcode(" ", BarcodeSymbology.QrCode),
            Throws.ArgumentException);
    }

    [Test]
    public void ItemInventory_TotalQuantity_RequiresPositiveValue()
    {
        var inventory = new ItemInventory(Guid.NewGuid(), 2);

        inventory.SetTotalQuantity(5);

        Assert.Multiple(() =>
        {
            Assert.That(inventory.TotalQuantity, Is.EqualTo(5));
            Assert.That(() => inventory.SetTotalQuantity(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inventory.SetTotalQuantity(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void Item_InventorySummary_CalculatesUnassignedQuantity()
    {
        var item = new Item("Hat", "Blue");

        var summary = new CoreApp.Domain.Entities.InventoryAggregate.InventorySnapshot(
            item,
            12,
            7,
            [new CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation(Guid.NewGuid(), "Box", 7)]);

        Assert.That(summary.UnassignedQuantity, Is.EqualTo(5));
    }

    [Test]
    public void Container_SetItemSummary_UpdatesCounts()
    {
        var container = new Container();

        container.SetItemSummary(itemTypeCount: 2, totalItemQuantity: 5);

        Assert.Multiple(() =>
        {
            Assert.That(container.ItemTypeCount, Is.EqualTo(2));
            Assert.That(container.TotalItemQuantity, Is.EqualTo(5));
        });
    }

    [Test]
    public void Container_SetItemSummary_WithNegativeValues_Throws()
    {
        var container = new Container();

        Assert.That(() => container.SetItemSummary(-1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => container.SetItemSummary(0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Container_Ctor_WithEmptyGuid_Throws()
    {
        Assert.That(() => new Container(Guid.Empty, "Name", "Notes"), Throws.ArgumentException);
    }

    [Test]
    public void Container_DefaultCtor_GeneratesNameFromId()
    {
        var container = new Container();

        Assert.That(container.Name, Is.EqualTo($"AutoGenerated: {container.ContainerId}"));
    }

    [Test]
    public void Container_Ctor_WithProvidedGuid_PreservesGuid()
    {
        var id = Guid.NewGuid();
        var c = new Container(id, "Name", "Notes");

        Assert.That(c.ContainerId, Is.EqualTo(id));
        Assert.That(c.Name, Is.EqualTo("Name"));
        Assert.That(c.Notes, Is.EqualTo("Notes"));
    }

    [Test]
    public void Container_Ctor_WithBlankName_Throws()
    {
        Assert.That(() => new Container(Guid.NewGuid(), " ", "Notes"), Throws.ArgumentException);
    }

    [Test]
    public void Container_UpdateDetails_WithNullNotes_NormalizesToEmpty()
    {
        var container = new Container(Guid.NewGuid(), "Name", "Notes");

        container.UpdateDetails("Name", null!);

        Assert.That(container.Notes, Is.Empty);
    }

    [Test]
    public void Container_UpdateDetails_WithBlankName_Throws()
    {
        var container = new Container(Guid.NewGuid(), "Name", "Notes");

        Assert.That(() => container.UpdateDetails("", "Notes"), Throws.ArgumentException);
    }

    [Test]
    public void Container_UpdateBarcode_AssignsBarcode()
    {
        var container = new Container(Guid.NewGuid(), "Name", "Notes");
        var barcode = new Barcode("container-42", BarcodeSymbology.Code128);

        container.UpdateBarcode(barcode);

        Assert.That(container.Barcode, Is.EqualTo(barcode));
    }

    [Test]
    public void Container_AddImageItem_WithExplicitImageId_AddsPhotoWithGivenId()
    {
        var container = new Container();
        var imageId = Guid.NewGuid();

        container.AddImageItem(imageId);

        Assert.That(container.Photos.Select(p => p.ImageId), Is.EquivalentTo(new[] { imageId }));
    }

    [Test]
    public void Container_AddImageItem_WithEmptyImageId_Throws()
    {
        var container = new Container();

        Assert.That(() => container.AddImageItem(Guid.Empty), Throws.ArgumentException);
    }

    [Test]
    public void Container_RemoveImageItem_RemovesAllMatchingPhotos_AndKeepsOthers()
    {
        var container = new Container();
        var removeId = Guid.NewGuid();
        var keepId = Guid.NewGuid();

        container.AddImageItem(removeId);
        container.AddImageItem(removeId);
        container.AddImageItem(keepId);

        container.RemoveImageItem(removeId);

        Assert.That(container.Photos.Select(p => p.ImageId), Is.EquivalentTo(new[] { keepId }));
    }

    [Test]
    public void Container_Collections_CannotBeMutatedDirectly()
    {
        var container = new Container();

        Assert.That(((ICollection<CoreApp.Domain.Entities.Shared.ImageItem>)container.Photos).IsReadOnly, Is.True);
    }

    [Test]
    public void Constants_Values_NotNull()
    {
        Assert.That(Constants.DataFolder, Is.Not.Null);
        Assert.That(Constants.InventoryFileName, Is.Not.Null);
    }
}
