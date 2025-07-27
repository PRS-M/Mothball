using CoreApp.Services.Interfaces;

namespace CoreApp;

public class Container
{
    public int Id { get; set; }
    public string UniqueId { get; set; }
    public string Name { get; set; }
    public string LocationDescription { get; set; }
    public string Description { get; set; }
    public List<Item> Items { get; set; }

    public void AddItem(Item item)
    {
        Items.Add(item);
    }

    public async Task AddItemPhoto(ICameraHandler cameraHandler, Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        await item.CapturePhotoAsync(cameraHandler, Name);
    }

    public void RemoveItem(Item item)
    {
        Items.Remove(item);
    }
}
