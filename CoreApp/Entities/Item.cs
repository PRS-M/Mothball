using CoreApp.Entities.Shared;
using CoreApp.Interfaces;

namespace CoreApp.Entities;

public class Item : BaseEntity, IAggregateRoot
{
    public Guid ItemId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ImageItem> Photos { get; set; } = new();

    public async Task CapturePhotoAsync(ICameraHandler cameraHandler, string containerId)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);
        ArgumentNullException.ThrowIfNull(containerId);

        // Capture photo logic using the camera handler
        ImageItem photoWithData = await cameraHandler.CaptureItemPhotoAsync(this, containerId);

        Photos.Add(photoWithData);
    }
}
