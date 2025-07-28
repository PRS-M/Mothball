using System;

namespace CoreApp.Services.Interfaces;

public interface ICameraHandler
{
    Task<Photo> CapturePhotoAsync(Item item, string containerName);
}
