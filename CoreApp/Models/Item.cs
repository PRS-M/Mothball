using System;
using System.Text.Json.Serialization;
using CoreApp.Services.Interfaces;

namespace CoreApp.Models;

public class Item
{
    public string UniqueId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public List<string> PhotoFileNames { get; set; } = new();
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
