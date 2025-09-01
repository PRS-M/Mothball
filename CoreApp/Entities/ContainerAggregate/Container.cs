using System.Text.Json.Serialization;
using CoreApp.Interfaces;

namespace CoreApp.Entities.ContainerAggregate;

public class Container : BaseEntity, IAggregateRoot
{
    public Container()
    {
        UniqueId = Guid.NewGuid().ToString();
        Name = string.Empty;
        LocationDescription = string.Empty;
        Description = string.Empty;
        Photos = new List<Photo>();
        Items = new List<StoredItem>();
    }

    public Container(string uniqueId, string name, string locationDescription, string description)
    {
        UniqueId = string.IsNullOrEmpty(uniqueId) ?
            Guid.NewGuid().ToString() :
            uniqueId;

        Name = name;
        LocationDescription = locationDescription;
        Description = description;
        Photos = new List<Photo>();
        Items = new List<StoredItem>();
    }

    [JsonConstructor]
    public Container(string uniqueId, string name, string locationDescription, string description, List<Photo> photos, List<StoredItem> items)
    {
        UniqueId = uniqueId;
        Name = name;
        LocationDescription = locationDescription;
        Description = description;
        Photos = photos;
        Items = items;
    }

    public string UniqueId { get; set; }
    public string Name { get; set; }
    public string LocationDescription { get; set; }
    public string Description { get; set; }
    public List<Photo> Photos { get; set; }
    public List<StoredItem> Items { get; set; }

    public void AddItem(StoredItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Items.Add(item);
    }

    public void AddPhoto(Photo photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        Photos.Add(photo);
    }

    public void RemoveItem(StoredItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Items.Remove(item);
    }

    public void RemovePhoto(Photo photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        Photos.Remove(photo);
    }

    public async Task CaptureContainerPhotoAsync(ICameraHandler cameraHandler)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);

        Photo photoWithData = await cameraHandler.CaptureContainerPhotoAsync(this);
        Photos.Add(photoWithData);
    }
}
