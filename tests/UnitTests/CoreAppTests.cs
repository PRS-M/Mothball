using CoreApp.Entities;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Services;
using CoreApp.Utilities;

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
    public void Container_AddItem_SameItemTwice_SumsQuantity_WithoutNewRow()
    {
        var container = new Container();
        var itemId = Guid.NewGuid();

        container.AddItem(itemId, 2);
        container.AddItem(itemId, 3);

        Assert.That(container.Items.Count, Is.EqualTo(1));
        Assert.That(container.Items[0].Quantity, Is.EqualTo(5));
        Assert.That(container.ItemCount, Is.EqualTo(5));
    }

    [Test]
    public void Container_AddItem_WithNonPositiveQuantity_Throws()
    {
        var container = new Container();
        var itemId = Guid.NewGuid();

        Assert.That(() => container.AddItem(itemId, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => container.AddItem(itemId, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Container_Ctor_WithEmptyGuid_GeneratesNewGuid()
    {
        var c = new Container(Guid.Empty, "Name", "Notes");
        Assert.That(c.ContainerId, Is.Not.EqualTo(Guid.Empty));
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
    public void Container_AddImageItem_WithExplicitImageId_AddsPhotoWithGivenId()
    {
        var container = new Container();
        var imageId = Guid.NewGuid();

        container.AddImageItem(imageId);

        Assert.That(container.Photos.Select(p => p.ImageId), Is.EquivalentTo(new[] { imageId }));
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
    public void Container_RemoveItem_RemovesAllMatchingRows_AndKeepsOthers()
    {
        var container = new Container();
        var removeId = Guid.NewGuid();
        var keepId = Guid.NewGuid();

        container.Items.Add(new StoredItem(removeId, 1));
        container.Items.Add(new StoredItem(removeId, 2));
        container.Items.Add(new StoredItem(keepId, 3));

        container.RemoveItem(removeId);

        Assert.That(container.Items.Select(i => i.ItemId), Is.EquivalentTo(new[] { keepId }));
        Assert.That(container.ItemCount, Is.EqualTo(3));
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
    public void StoredItem_AddQuantity_WithNonPositiveValue_Throws()
    {
        var stored = new StoredItem(Guid.NewGuid(), 1);

        Assert.That(() => stored.AddQuantity(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => stored.AddQuantity(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constants_Values_NotNull()
    {
        Assert.That(Constants.DataFolder, Is.Not.Null);
        Assert.That(Constants.InventoryFileName, Is.Not.Null);
    }
}


