using System;
using System.Text.Json.Serialization;
using CoreApp.Services.Interfaces;

namespace CoreApp;

public class Item
{
    public string Name { get; set; }
    public List<string> PhotoFileNames { get; set; }
    public List<Photo> Photos { get; set; }

    [JsonIgnore]
    public List<Photo> PhotosWithData { get; set; }

    public async Task CapturePhotoAsync(ICameraHandler cameraHandler, string containerName)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);
        ArgumentNullException.ThrowIfNull(containerName);

        // Capture photo logic using the camera handler
        Photo photoWithData = await cameraHandler.CaptureItemPhotoAsync(this, containerName);

        PhotosWithData.Add(photoWithData);
        Photo photo = new Photo { FileName = photoWithData.FileName };
        Photos.Add(photo);
    }
}
