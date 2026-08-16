using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;

namespace Mothball.Tests.Unit.Infrastructure.Mappers;

[TestFixture]
public class MapperTests
{
    [Test]
    public void ContainerMapper_ToDb_MapsCoreFields()
    {
        var id = Guid.NewGuid();
        var c = new Container(id, "Name", "Notes");

        var db = c.ToDb();

        Assert.That(db.ContainerId, Is.EqualTo(id));
        Assert.That(db.Name, Is.EqualTo("Name"));
        Assert.That(db.Notes, Is.EqualTo("Notes"));
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

        Assert.That(domain.ContainerId, Is.EqualTo(containerId));
        Assert.That(domain.Items.Count, Is.EqualTo(1));
        Assert.That(domain.Items[0].ItemId, Is.EqualTo(itemId));
        Assert.That(domain.Items[0].Quantity, Is.EqualTo(5));
    }

    [Test]
    public void ContainerMapper_ToDomain_ConvertsPhotos_ByImageId()
    {
        var containerId = Guid.NewGuid();
        var db = new DbContainer { ContainerId = containerId, Name = "C", Notes = "N" };

        var p1 = new DbImage { ImageId = Guid.NewGuid(), OwnerUniqueId = containerId };
        var p2 = new DbImage { ImageId = Guid.NewGuid(), OwnerUniqueId = containerId };

        var domain = db.ToDomain(photos: new[] { p1, p2 });

        Assert.That(domain.Photos.Select(p => p.ImageId), Is.EquivalentTo(new[] { p1.ImageId, p2.ImageId }));
    }

    [Test]
    public void ItemMapper_ToDb_MapsCoreFields()
    {
        var id = Guid.NewGuid();
        var item = new Item(id, "Hat", "Desc");

        var db = item.ToDb();

        Assert.That(db.ItemId, Is.EqualTo(id));
        Assert.That(db.Name, Is.EqualTo("Hat"));
        Assert.That(db.Description, Is.EqualTo("Desc"));
    }

    [Test]
    public void ItemMapper_ToDomain_ConvertsPhotos_WhenProvided()
    {
        var itemId = Guid.NewGuid();
        var db = new DbItem { ItemId = itemId, Name = "Hat", Description = "Desc" };

        var p1 = new DbImage { ImageId = Guid.NewGuid(), OwnerUniqueId = itemId };

        var domain = db.ToDomain(new[] { p1 });

        Assert.That(domain.ItemId, Is.EqualTo(itemId));
        Assert.That(domain.Photos.Select(p => p.ImageId), Is.EquivalentTo(new[] { p1.ImageId }));
    }

    [Test]
    public void ImageMapper_ToDb_Throws_OnEmptyOwnerId()
    {
        var img = new ImageItem(Guid.NewGuid());
        Assert.That(() => img.ToDb(Guid.Empty), Throws.ArgumentException);
    }

    [Test]
    public void ImageMapper_ToDomain_UsesImageId()
    {
        var id = Guid.NewGuid();
        var db = new DbImage { ImageId = id, OwnerUniqueId = Guid.NewGuid() };

        var domain = db.ToDomain();

        Assert.That(domain.ImageId, Is.EqualTo(id));
    }
}
