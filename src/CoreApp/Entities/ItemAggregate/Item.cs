using CoreApp.Entities.Shared;

namespace CoreApp.Entities.ItemAggregate;

public class Item : BaseEntity, IAggregateRoot
{
    private readonly List<ImageItem> photos = new();

    public Item()
        : this(Guid.NewGuid(), string.Empty, string.Empty)
    {
    }

    public Item(string name, string description)
        : this(Guid.NewGuid(), name, description)
    {
    }

    public Item(Guid itemId, string name, string description)
    {
        ItemId = itemId == Guid.Empty ? Guid.NewGuid() : itemId;
        UpdateDetails(name, description);
    }

    public Guid ItemId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public IReadOnlyList<ImageItem> Photos => photos.AsReadOnly();

    public void UpdateDetails(string name, string description)
    {
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
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
