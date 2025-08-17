using System.Text.Json.Serialization;
using CoreApp.Services.Interfaces;

namespace CoreApp;

public class Container
{
    public Container(string uniqueId, string name, string locationDescription, string description)
    {
        UniqueId = string.IsNullOrEmpty(uniqueId) ? Guid.NewGuid().ToString() : uniqueId;
        Name = name;
        LocationDescription = locationDescription;
        Description = description;
        Items = new List<Item>();
        Photo = new Photo();
    }

    [JsonConstructor]
    public Container(string uniqueId, string name, string locationDescription, string description, Photo photo, List<Item>? items)
    {
        UniqueId = uniqueId;
        Name = name;
        LocationDescription = locationDescription;
        Description = description;
        Photo = photo;
        Items = items ?? new List<Item>();
    }

    public string UniqueId { get; set; }
    public string Name { get; set; }
    public string LocationDescription { get; set; }
    public string Description { get; set; }
    public Photo Photo { get; set; }
    public List<Item> Items { get; set; }

    public void AddItem(Item item)
    {
        Items.Add(item);
    }

    public async Task CaptureContainerPhotoAsync(ICameraHandler cameraHandler)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);

        Photo photoWithData = await cameraHandler.CaptureContainerPhotoAsync(this);
        Photo = photoWithData;
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
