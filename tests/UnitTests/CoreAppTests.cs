using CoreApp.Entities;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Services;
using CoreApp.Utilities;
using Moq;
using CoreApp.Interfaces;

namespace UnitTests;

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
    public void Container_AddItem_AddsQuantity()
    {
        var container = new Container();
        var itemId = Guid.NewGuid();
        container.AddItem(itemId, 2);

        Assert.Multiple(() =>
        {
            Assert.That(container.Items.Count, Is.EqualTo(1));
            Assert.That(container.ItemCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void StoredItem_Requires_Valid_ItemId_And_Quantity()
    {
        Assert.That(() => new StoredItem(Guid.Empty, 1), Throws.ArgumentException);
        Assert.That(() => new StoredItem(Guid.NewGuid(), 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new StoredItem(Guid.NewGuid(), -1), Throws.TypeOf<ArgumentOutOfRangeException>());

        var stored = new StoredItem(Guid.NewGuid(), 1);
        stored.AddQuantity(2);
        Assert.That(stored.Quantity, Is.EqualTo(3));

    }

    [Test]
    public void Constants_Values_NotNull()
    {
        Assert.That(Constants.DataFolder, Is.Not.Null);
        Assert.That(Constants.InventoryFileName, Is.Not.Null);
    }
}


