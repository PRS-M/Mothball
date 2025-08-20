using CoreApp.Models;
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
            PhotoFileName = container.Photo?.FileName
        };
    }

    public static Container ToDomain(this DbContainer dbContainer, Photo? photo = null)
    {
        ArgumentNullException.ThrowIfNull(dbContainer);
        var result = new Container(
            dbContainer.UniqueId,
            dbContainer.Name,
            dbContainer.LocationDescription,
            dbContainer.Description
        );

        // Prefer provided photo (with data) else instantiate from stored file name
        if (photo is not null)
        {
            result.Photo = photo;
        }
        else if (!string.IsNullOrWhiteSpace(dbContainer.PhotoFileName))
        {
            result.Photo = new Photo(dbContainer.PhotoFileName);
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
            ContainerId = containerId
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
                item.PhotoFileNames.Add(p.FileName);
                item.Photos.Add(new Photo(p.FileName));
                if (p.ImageData is not null)
                {
                    item.PhotosWithData.Add(new Photo(p.FileName, p.ImageData));
                }
            }
        }

        return item;
    }
}

public static class PhotoMapper
{
    public static DbPhoto ToDbForContainer(this Photo photo, string containerId)
    {
        ArgumentNullException.ThrowIfNull(photo);
        ArgumentNullException.ThrowIfNull(containerId);
        return new DbPhoto
        {
            ContainerId = containerId,
            FileName = photo.FileName,
            ImageData = photo.ImageData
        };
    }

    public static DbPhoto ToDbForItem(this Photo photo, string itemId)
    {
        ArgumentNullException.ThrowIfNull(photo);
        ArgumentNullException.ThrowIfNull(itemId);
        return new DbPhoto
        {
            ItemId = itemId,
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
