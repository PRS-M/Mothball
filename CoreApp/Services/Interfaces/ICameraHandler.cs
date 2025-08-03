using System;

namespace CoreApp.Services.Interfaces;

public interface ICameraHandler
{
    Task<Photo> CaptureContainerPhotoAsync(Container container);
    Task<Photo> CaptureItemPhotoAsync(Item item, string containerName);
}
