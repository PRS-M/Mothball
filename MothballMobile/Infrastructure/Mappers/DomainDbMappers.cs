using System.Linq;
using CoreApp.Entities;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.Shared;
using MothballMobile.Infrastructure.DatabaseModels;

namespace MothballMobile.Infrastructure.Mappers;

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

    public static Container ToDomain(this DbContainer dbContainer, IEnumerable<DbImage>? photos = null)
    {
        ArgumentNullException.ThrowIfNull(dbContainer);
        var result = new Container(
            dbContainer.ContainerId.ToString(),
            dbContainer.Name,
            dbContainer.Notes
        );

        if (photos is not null && photos.Any())
        {
            List<ImageItem> convertedPhotos = [.. photos.Select(p => p.ToDomain())];
            result.Photos.AddRange(convertedPhotos);
        }

        return result;
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
        };
    }

    public static Item ToDomain(this DbItem dbItem, IEnumerable<DbImage>? dbPhotos = null)
    {
        ArgumentNullException.ThrowIfNull(dbItem);
        var item = new Item
        {
            ItemId = dbItem.ItemId,
            Name = dbItem.Name,
        };

        if (dbPhotos is not null)
        {
            foreach (var p in dbPhotos.Where(p => !string.IsNullOrWhiteSpace(p.FileName)))
            {
                item.Photos.Add(new ImageItem(Guid.Parse(p.FileName)));
            }
        }

        return item;
    }
}

public static class ImageMapper
{
    public static DbImage ToDb(this ImageItem photo, string ownerId)
    {
        ArgumentNullException.ThrowIfNull(photo);
        ArgumentNullException.ThrowIfNull(ownerId);
        return new DbImage
        {
            OwnerUniqueId = Guid.Parse(ownerId),
        };
    }

    public static ImageItem ToDomain(this DbImage dbPhoto)
    {
        ArgumentNullException.ThrowIfNull(dbPhoto);
        return new ImageItem(dbPhoto.ImageId);
    }
}
