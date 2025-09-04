using CoreApp.Entities.Shared;
using CoreApp.Interfaces;

namespace CoreApp.Entities.ItemAggregate;

public class Item : BaseEntity, IAggregateRoot
{
    public Guid ItemId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ImageItem> Photos { get; set; } = new();

    public ImageItem AddImageItem()
    {
        var newImage = new ImageItem();
        Photos.Add(newImage);
        return newImage;
    }

    public void RemoveImageItem(Guid imageId)
    {
        Photos.RemoveAll(p => p.ImageId == imageId);
    }
}
