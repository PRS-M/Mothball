using System;
using System.Text.Json.Serialization;
using CoreApp.Services.Interfaces;

namespace CoreApp;

public class Item
{
    public string Name { get; set; } = string.Empty;
    public List<string> PhotoFileNames { get; set; } = new();
    public List<Photo> Photos { get; set; } = new();

    [JsonIgnore]
    public List<Photo> PhotosWithData { get; set; } = new();

    public async Task CapturePhotoAsync(ICameraHandler cameraHandler, string containerName)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);
        ArgumentNullException.ThrowIfNull(containerName);

        // Capture photo logic using the camera handler
        Photo photoWithData = await cameraHandler.CaptureItemPhotoAsync(this, containerName);

        PhotosWithData.Add(photoWithData);
        var photo = new Photo { FileName = photoWithData.FileName };
        Photos.Add(photo);
    }
}
