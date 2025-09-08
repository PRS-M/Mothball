using System;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;

namespace CoreApp.Interfaces;

public interface ICameraHandler
{
    Task<ImageItem> CaptureContainerPhotoAsync(Container container);
    Task<ImageItem> CaptureItemPhotoAsync(Item item);
}
