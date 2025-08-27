using System.Text.Json.Serialization;
using CoreApp.Services.Interfaces;

namespace CoreApp.Models;

public class Container
{
    public Container()
    {
        UniqueId = Guid.NewGuid().ToString();
        Name = string.Empty;
        LocationDescription = string.Empty;
        Description = string.Empty;
        Photos = new List<Photo>();
    }

    public Container(string uniqueId, string name, string locationDescription, string description)
    {
        UniqueId = string.IsNullOrEmpty(uniqueId) ? Guid.NewGuid().ToString() : uniqueId;
        Name = name;
        LocationDescription = locationDescription;
        Description = description;
        Photos = new List<Photo>();
    }

    [JsonConstructor]
    public Container(string uniqueId, string name, string locationDescription, string description, List<Photo> photos)
    {
        UniqueId = uniqueId;
        Name = name;
        LocationDescription = locationDescription;
        Description = description;
        Photos = photos;
    }

    public string UniqueId { get; set; }
    public string Name { get; set; }
    public string LocationDescription { get; set; }
    public string Description { get; set; }
    public List<Photo> Photos { get; set; }

    public async Task CaptureContainerPhotoAsync(ICameraHandler cameraHandler)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);

        Photo photoWithData = await cameraHandler.CaptureContainerPhotoAsync(this);
        Photos.Add(photoWithData);
    }

    public async Task AddItemPhoto(ICameraHandler cameraHandler, Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        await item.CapturePhotoAsync(cameraHandler, UniqueId);
    }
}
