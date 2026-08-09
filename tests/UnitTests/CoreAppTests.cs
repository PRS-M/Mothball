using CoreApp.Entities;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using FluentAssertions;
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
        entity.Id.Should().Be(0);
    }

    [Test]
    public void Item_AddImageItem_AddsPhoto()
    {
        var item = new Item();
        var countBefore = item.Photos.Count;
        item.AddImageItem();
        item.Photos.Count.Should().Be(countBefore + 1);
    }

    [Test]
    public void Item_RemoveImageItem_RemovesPhoto()
    {
        var item = new Item();
        var img = item.AddImageItem();
        item.RemoveImageItem(img.ImageId);
        item.Photos.Should().NotContain(img);
    }

    [Test]
    public void Container_AddItem_AddsQuantity()
    {
        var container = new Container();
        var itemId = Guid.NewGuid();
        container.AddItem(itemId, 2);

        container.Items.Count.Should().Be(1);
        container.ItemCount.Should().Be(2);
    }

    [Test]
    public void Container_AddItem_SameItemTwice_SumsQuantity_WithoutNewRow()
    {
        var container = new Container();
        var itemId = Guid.NewGuid();

        container.AddItem(itemId, 2);
        container.AddItem(itemId, 3);

        container.Items.Count.Should().Be(1);
        container.Items[0].Quantity.Should().Be(5);
        container.ItemCount.Should().Be(5);
    }

    [Test]
    public void Container_Ctor_WithEmptyGuid_GeneratesNewGuid()
    {
        var c = new Container(Guid.Empty, "Name", "Notes");
        c.ContainerId.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void StoredItem_Requires_Valid_ItemId_And_Quantity()
    {
        FluentActions.Invoking(() => new StoredItem(Guid.Empty, 1)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new StoredItem(Guid.NewGuid(), 0)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new StoredItem(Guid.NewGuid(), -1)).Should().Throw<ArgumentOutOfRangeException>();

        var stored = new StoredItem(Guid.NewGuid(), 1);
        stored.AddQuantity(2);
        stored.Quantity.Should().Be(3);

    }

    [Test]
    public void Constants_Values_NotNull()
    {
        Constants.DataFolder.Should().NotBeNull();
        Constants.InventoryFileName.Should().NotBeNull();
    }
}


