using System.Linq;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Mappers;

public static class ContainerMapper
{
    public static DbContainer ToDb(this Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return new DbContainer
        {
            ContainerId = container.ContainerId,
            Name = container.Name,
            Notes = container.Notes,
        };
    }

    public static Container ToDomain(this DbContainer dbContainer, IEnumerable<DbItemContainerRelation>? relations = null)
    {
        Container result = CreateContainer(dbContainer);
        ConvertAndAddRelations(relations, result);

        return result;
    }

    public static Container ToDomain(this DbContainer dbContainer, IEnumerable<DbImage>? photos, IEnumerable<DbItemContainerRelation>? relations)
    {
        Container result = CreateContainer(dbContainer);
        ConvertAndAddPhotos(photos, result);
        ConvertAndAddRelations(relations, result);
        return result;
    }

    public static Container ToDomain(this DbContainer dbContainer, IEnumerable<DbImage>? photos = null)
    {
        Container result = CreateContainer(dbContainer);
        ConvertAndAddPhotos(photos, result);

        return result;
    }

    private static Container CreateContainer(DbContainer dbContainer)
    {
        ArgumentNullException.ThrowIfNull(dbContainer);
        var result = new Container(
            dbContainer.ContainerId,
            dbContainer.Name,
            dbContainer.Notes
        );

        return result;
    }

    private static void ConvertAndAddPhotos(IEnumerable<DbImage>? photos, Container result)
    {
        if (photos is not null && photos.Any())
        {
            List<ImageItem> convertedPhotos = [.. photos.Select(p => p.ToDomain())];
            result.Photos.AddRange(convertedPhotos);
        }
    }

    private static void ConvertAndAddRelations(IEnumerable<DbItemContainerRelation>? relations, Container result)
    {
        if (relations is null) return;

        foreach (var group in relations.Where(r => r.Quantity > 0).GroupBy(r => r.ItemId))
        {
            result.AddItem(group.Key, group.Sum(r => r.Quantity));
        }
    }
}

public static class ItemMapper
{
    public static DbItem ToDb(this Item item, string? containerId = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new DbItem
        {
            ItemId = item.ItemId,
            Name = item.Name,
            Description = item.Description,
        };
    }

    public static Item ToDomain(this DbItem dbItem, IEnumerable<DbImage>? dbPhotos = null)
    {
        ArgumentNullException.ThrowIfNull(dbItem);
        var item = new Item
        {
            ItemId = dbItem.ItemId,
            Name = dbItem.Name,
            Description = dbItem.Description,
        };

        if (dbPhotos is not null)
        {
            foreach (var p in dbPhotos.Where(p => !string.IsNullOrWhiteSpace(p.FileName)))
            {
                // Use the ImageId directly; FileName includes an extension and is not a valid GUID string
                item.Photos.Add(p.ToDomain());
            }
        }

        return item;
    }
}

public static class ImageMapper
{
    public static DbImage ToDb(this ImageItem photo, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(photo);
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));
        }

        return new DbImage
        {
            ImageId = photo.ImageId,
            OwnerUniqueId = ownerId,
        };
    }

    public static ImageItem ToDomain(this DbImage dbPhoto)
    {
        ArgumentNullException.ThrowIfNull(dbPhoto);
        return new ImageItem(dbPhoto.ImageId);
    }
}
