using CoreApp.Abstractions.Domain;
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

    /// <summary>
    /// Updates the item's name and description.
    /// </summary>
    /// <param name="name">The new item name.</param>
    /// <param name="description">The new item description.</param>
    public void UpdateDetails(string name, string description)
    {
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Creates and adds a new image to the item.
    /// </summary>
    /// <returns>The image added to the item.</returns>
    public ImageItem AddImageItem()
    {
        var newImage = new ImageItem();
        photos.Add(newImage);
        return newImage;
    }

    /// <summary>
    /// Creates and adds an image with the specified identifier to the item.
    /// </summary>
    /// <param name="imageId">The identifier for the image.</param>
    /// <returns>The image added to the item.</returns>
    public ImageItem AddImageItem(Guid imageId)
    {
        var image = new ImageItem(imageId);
        photos.Add(image);
        return image;
    }

    /// <summary>
    /// Adds images to the item.
    /// </summary>
    /// <param name="imageItems">The images to add.</param>
    public void AddImageItems(IEnumerable<ImageItem> imageItems)
    {
        ArgumentNullException.ThrowIfNull(imageItems);
        photos.AddRange(imageItems);
    }

    /// <summary>
    /// Removes an image from the item.
    /// </summary>
    /// <param name="imageId">The identifier of the image to remove.</param>
    public void RemoveImageItem(Guid imageId)
    {
        photos.RemoveAll(p => p.ImageId == imageId);
    }
}