using System;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;

namespace CoreApp.Interfaces;

/// <summary>
/// Provides camera functionality for capturing photos of containers and items.
/// Handles the complete workflow from photo capture to creating associated ImageItem entities.
/// </summary>
public interface ICameraHandler
{
    /// <summary>
    /// Captures a photo for the specified container and creates an associated ImageItem.
    /// The photo is automatically saved to the appropriate storage location.
    /// </summary>
    /// <param name="container">The container to associate the photo with.</param>
    /// <returns>A new ImageItem representing the captured photo.</returns>
    /// <exception cref="ArgumentNullException">Thrown when container is null.</exception>
    Task<ImageItem> CaptureContainerPhotoAsync(Container container);

    /// <summary>
    /// Captures a photo for the specified item and creates an associated ImageItem.
    /// The photo is automatically saved to the appropriate storage location.
    /// </summary>
    /// <param name="item">The item to associate the photo with.</param>
    /// <returns>A new ImageItem representing the captured photo.</returns>
    /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
    Task<ImageItem> CaptureItemPhotoAsync(Item item);
}
