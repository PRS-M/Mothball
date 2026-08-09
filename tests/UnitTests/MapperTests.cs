using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using FluentAssertions;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;

namespace UnitTests;

[TestFixture]
public class MapperTests
{
    [Test]
    public void ContainerMapper_ToDb_MapsCoreFields()
    {
        var id = Guid.NewGuid();
        var c = new Container(id, "Name", "Notes");

        var db = c.ToDb();

        db.ContainerId.Should().Be(id);
        db.Name.Should().Be("Name");
        db.Notes.Should().Be("Notes");
    }

    [Test]
    public void ContainerMapper_ToDomain_SumsRelationQuantities_AndIgnoresNonPositive()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var db = new DbContainer { ContainerId = containerId, Name = "C", Notes = "N" };

        var relations = new List<DbItemContainerRelation>
        {
            new() { ItemId = itemId, ContainerId = containerId, Quantity = 2 },
            new() { ItemId = itemId, ContainerId = containerId, Quantity = 3 },
            new() { ItemId = itemId, ContainerId = containerId, Quantity = 0 },
            new() { ItemId = Guid.NewGuid(), ContainerId = containerId, Quantity = -10 },
        };

        var domain = db.ToDomain(relations);

        domain.ContainerId.Should().Be(containerId);
        domain.Items.Count.Should().Be(1);
        domain.Items[0].ItemId.Should().Be(itemId);
        domain.Items[0].Quantity.Should().Be(5);
    }

    [Test]
    public void ContainerMapper_ToDomain_ConvertsPhotos_ByImageId()
    {
        var containerId = Guid.NewGuid();
        var db = new DbContainer { ContainerId = containerId, Name = "C", Notes = "N" };

        var p1 = new DbImage { ImageId = Guid.NewGuid(), OwnerUniqueId = containerId };
        var p2 = new DbImage { ImageId = Guid.NewGuid(), OwnerUniqueId = containerId };

        var domain = db.ToDomain(photos: new[] { p1, p2 });

        domain.Photos.Select(p => p.ImageId).Should().BeEquivalentTo(new[] { p1.ImageId, p2.ImageId });
    }

    [Test]
    public void ItemMapper_ToDb_MapsCoreFields()
    {
        var id = Guid.NewGuid();
        var item = new Item { ItemId = id, Name = "Hat", Description = "Desc" };

        var db = item.ToDb();

        db.ItemId.Should().Be(id);
        db.Name.Should().Be("Hat");
        db.Description.Should().Be("Desc");
    }

    [Test]
    public void ItemMapper_ToDomain_ConvertsPhotos_WhenProvided()
    {
        var itemId = Guid.NewGuid();
        var db = new DbItem { ItemId = itemId, Name = "Hat", Description = "Desc" };

        var p1 = new DbImage { ImageId = Guid.NewGuid(), OwnerUniqueId = itemId };

        var domain = db.ToDomain(new[] { p1 });

        domain.ItemId.Should().Be(itemId);
        domain.Photos.Select(p => p.ImageId).Should().BeEquivalentTo(new[] { p1.ImageId });
    }

    [Test]
    public void ImageMapper_ToDb_Throws_OnEmptyOwnerId()
    {
        var img = new ImageItem(Guid.NewGuid());
        FluentActions.Invoking(() => img.ToDb(Guid.Empty)).Should().Throw<ArgumentException>();
    }

    [Test]
    public void ImageMapper_ToDomain_UsesImageId()
    {
        var id = Guid.NewGuid();
        var db = new DbImage { ImageId = id, OwnerUniqueId = Guid.NewGuid() };

        var domain = db.ToDomain();

        domain.ImageId.Should().Be(id);
    }
}
