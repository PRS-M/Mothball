using System;
using CoreApp.Entities;

namespace CoreApp.Interfaces;

public interface ICameraHandler
{
    Task<Photo> CaptureContainerPhotoAsync(Container container);
    Task<Photo> CaptureItemPhotoAsync(Item item, string containerId);
}
