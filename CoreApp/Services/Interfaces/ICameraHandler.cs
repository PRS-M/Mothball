using System;

namespace CoreApp.Services.Interfaces;

public interface ICameraHandler
{
    Task<PhotoWithData> CapturePhotoAsync(Item item, string containerName);
}
