using System.Linq;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
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
            dbContainer.ContainerId,
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
