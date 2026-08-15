using CoreApp.Entities.Shared;
using CoreApp.Interfaces;

namespace CoreApp.Entities.ItemAggregate;

public class Item : BaseEntity, IAggregateRoot
{
    private readonly List<ImageItem> photos = new();

    public Item()
        : this(Guid.NewGuid(), string.Empty, string.Empty, 1)
    {
    }

    public Item(string name, string description, int totalQuantity = 1)
        : this(Guid.NewGuid(), name, description, totalQuantity)
    {
    }

    public Item(Guid itemId, string name, string description, int totalQuantity = 1)
    {
        ItemId = itemId == Guid.Empty ? Guid.NewGuid() : itemId;
        UpdateDetails(name, description);
        SetTotalQuantity(totalQuantity);
    }

    public Guid ItemId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int TotalQuantity { get; private set; }
    public IReadOnlyList<ImageItem> Photos => photos.AsReadOnly();

    public void UpdateDetails(string name, string description)
    {
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public void SetTotalQuantity(int totalQuantity)
    {
        if (totalQuantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalQuantity), "Total quantity must be at least one.");
        }

        TotalQuantity = totalQuantity;
    }

    public ImageItem AddImageItem()
    {
        var newImage = new ImageItem();
        photos.Add(newImage);
        return newImage;
    }

    public ImageItem AddImageItem(Guid imageId)
    {
        var image = new ImageItem(imageId);
        photos.Add(image);
        return image;
    }

    public void AddImageItems(IEnumerable<ImageItem> imageItems)
    {
        ArgumentNullException.ThrowIfNull(imageItems);
        photos.AddRange(imageItems);
    }

    public void RemoveImageItem(Guid imageId)
    {
        photos.RemoveAll(p => p.ImageId == imageId);
    }
}
