using System;

namespace CoreApp.Services.Interfaces;

public interface ICameraHandler
{
    Task<Photo> CaptureItemPhotoAsync(Item item, string containerName);
}
