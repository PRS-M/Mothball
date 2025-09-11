using System;
using CoreApp;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Utilities;

namespace Infrastructure.Services;

/// <summary>
/// Mobile implementation of camera functionality using MAUI's media picker.
/// </summary>
public class CameraHandler : ICameraHandler
{
    private readonly IMediaPicker mediaPicker;
    private readonly IFileHandler fileHandler;

    public CameraHandler(IMediaPicker mediaPicker, IFileHandler fileHandler)
    {
        this.mediaPicker = mediaPicker ?? throw new ArgumentNullException(nameof(mediaPicker));
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
    }

    /// <inheritdoc />
    public async Task<ImageItem> CaptureContainerPhotoAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return await CaptureAndSavePhotoAsync(container.AddImageItem, Constants.PathToContainerPhotos);
    }

    /// <inheritdoc />
    public async Task<ImageItem> CaptureItemPhotoAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return await CaptureAndSavePhotoAsync(item.AddImageItem, Constants.PathToItemPhotos);
    }

    // Consolidated helper: creates the ImageItem, captures bytes, and saves if non-empty.
    private async Task<ImageItem> CaptureAndSavePhotoAsync(Func<ImageItem> imageItemFactory, string pathPrefix)
    {
        ArgumentNullException.ThrowIfNull(imageItemFactory);
        ImageItem imageItem = imageItemFactory();

        try
        {
            byte[] bytes = await CapturePhotoAsync();
            if (bytes.Length > 0)
            {
                await fileHandler.SaveFileAsync(imageItem.FileName, pathPrefix, bytes);
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user cancels, permissions denied)
            Console.WriteLine($"Error capturing photo: {ex.Message}");
        }

        return imageItem;
    }

    private async Task<byte[]> CapturePhotoAsync()
    {
        FileResult? photo = await mediaPicker.PickPhotoAsync();
        if (photo == null) return Array.Empty<byte>();

        using Stream stream = await photo.OpenReadAsync();

        // If the stream is seekable and the length is known, read directly into a pre-sized buffer.
        if (stream.CanSeek)
        {
            long length = stream.Length;
            if (length <= 0) return Array.Empty<byte>();

            byte[] buffer = new byte[length];
            await stream.ReadExactlyAsync(buffer, 0, (int)length);
            return buffer;
        }

        // Fallback for non-seekable streams.
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}
