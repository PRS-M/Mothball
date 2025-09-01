using CoreApp.Interfaces;

namespace CoreApp.Entities;

public class Item
{
    public string UniqueId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Photo> Photos { get; set; } = new();

    public async Task CapturePhotoAsync(ICameraHandler cameraHandler, string containerId)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);
        ArgumentNullException.ThrowIfNull(containerId);

        // Capture photo logic using the camera handler
        Photo photoWithData = await cameraHandler.CaptureItemPhotoAsync(this, containerId);

        Photos.Add(photoWithData);
    }
}
