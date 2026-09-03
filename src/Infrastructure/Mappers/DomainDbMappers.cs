using System.Linq;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.Entities.Shared;
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
            BarcodeValue = container.Barcode?.Value ?? string.Empty,
            BarcodeSymbology = container.Barcode is null ? null : (int)container.Barcode.Symbology,
        };
    }

    public static Container ToDomain(this DbContainer dbContainer, IEnumerable<DbItemContainerRelation>? relations = null)
    {
        Container result = CreateContainer(dbContainer);
        ApplyItemSummary(relations, result);

        return result;
    }

    public static Container ToDomain(this DbContainer dbContainer, IEnumerable<DbImage>? photos, IEnumerable<DbItemContainerRelation>? relations)
    {
        Container result = CreateContainer(dbContainer);
        ConvertAndAddPhotos(photos, result);
        ApplyItemSummary(relations, result);
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
        result.UpdateBarcode(CreateBarcode(dbContainer.BarcodeValue, dbContainer.BarcodeSymbology));

        return result;
    }

    private static void ConvertAndAddPhotos(IEnumerable<DbImage>? photos, Container result)
    {
        if (photos is not null)
        {
            result.AddImageItems(photos.Select(p => p.ToDomain()));
        }
    }

    private static void ApplyItemSummary(IEnumerable<DbItemContainerRelation>? relations, Container result)
    {
        if (relations is null) return;

        var positive = relations.Where(r => r.Quantity > 0).ToList();
        result.SetItemSummary(
            itemTypeCount: positive.Select(r => r.ItemId).Distinct().Count(),
            totalItemQuantity: positive.Sum(r => r.Quantity));
    }

    private static Barcode? CreateBarcode(string? value, int? symbology)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (symbology is null || !Enum.IsDefined(typeof(BarcodeSymbology), symbology.Value))
        {
            throw new InvalidOperationException("Stored container barcode symbology is invalid.");
        }

        return new Barcode(value, (BarcodeSymbology)symbology.Value);
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
            BarcodeValue = item.Barcode?.Value ?? string.Empty,
            BarcodeSymbology = item.Barcode is null ? null : (int)item.Barcode.Symbology,
        };
    }

    public static Item ToDomain(this DbItem dbItem, IEnumerable<DbImage>? dbPhotos = null)
    {
        ArgumentNullException.ThrowIfNull(dbItem);
        var item = new Item(dbItem.ItemId, dbItem.Name, dbItem.Description);
        item.UpdateBarcode(CreateBarcode(dbItem.BarcodeValue, dbItem.BarcodeSymbology));

        if (dbPhotos is not null)
        {
            item.AddImageItems(dbPhotos
                .Where(p => !string.IsNullOrWhiteSpace(p.FileName))
                .Select(p => p.ToDomain()));
        }

        return item;
    }

    private static Barcode? CreateBarcode(string? value, int? symbology)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (symbology is null || !Enum.IsDefined(typeof(BarcodeSymbology), symbology.Value))
        {
            throw new InvalidOperationException("Stored item barcode symbology is invalid.");
        }

        return new Barcode(value, (BarcodeSymbology)symbology.Value);
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
