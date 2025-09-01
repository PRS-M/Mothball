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
            UniqueId = container.UniqueId,
            Name = container.Name,
            LocationDescription = container.LocationDescription,
            Description = container.Description,
        };
    }

    public static Container ToDomain(this DbContainer dbContainer, IEnumerable<DbPhoto>? photos = null)
    {
        ArgumentNullException.ThrowIfNull(dbContainer);
        var result = new Container(
            dbContainer.UniqueId,
            dbContainer.Name,
            dbContainer.LocationDescription,
            dbContainer.Description
        );

        if (photos is not null && photos.Any())
        {
            List<Photo> convertedPhotos = [.. photos.Select(p => p.ToDomain())];
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
            UniqueId = item.UniqueId,
            Name = item.Name,
        };
    }

    public static Item ToDomain(this DbItem dbItem, IEnumerable<DbPhoto>? dbPhotos = null)
    {
        ArgumentNullException.ThrowIfNull(dbItem);
        var item = new Item
        {
            UniqueId = dbItem.UniqueId,
            Name = dbItem.Name,
        };

        if (dbPhotos is not null)
        {
            foreach (var p in dbPhotos.Where(p => !string.IsNullOrWhiteSpace(p.FileName)))
            {
                item.Photos.Add(new Photo(p.FileName));
                if (p.ImageData is not null)
                {
                    item.Photos.Add(new Photo(p.FileName, p.ImageData));
                }
            }
        }

        return item;
    }
}

public static class PhotoMapper
{
    public static DbPhoto ToDbForContainer(this Photo photo, string ownerId)
    {
        ArgumentNullException.ThrowIfNull(photo);
        ArgumentNullException.ThrowIfNull(ownerId);
        return new DbPhoto
        {
            OwnerUniqueId = ownerId,
            FileName = photo.FileName,
            ImageData = photo.ImageData
        };
    }

    public static Photo ToDomain(this DbPhoto dbPhoto)
    {
        ArgumentNullException.ThrowIfNull(dbPhoto);
        return new Photo(dbPhoto.FileName, dbPhoto.ImageData);
    }
}
