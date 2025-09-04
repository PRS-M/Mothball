using System;
using CoreApp;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Utilities;

namespace Infrastructure.Services;

public class CameraHandler : ICameraHandler
{
    private readonly IMediaPicker mediaPicker;
    private readonly IFileHandler fileHandler;

    public CameraHandler(IMediaPicker mediaPicker, IFileHandler fileHandler)
    {
        this.mediaPicker = mediaPicker ?? throw new ArgumentNullException(nameof(mediaPicker));
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
    }

    public async Task<ImageItem> CaptureContainerPhotoAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        var imageItem = new ImageItem();

        try
        {
            await CaptureAndSavePhoto(imageItem.FileName, Constants.PathToContainerPhotos);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user cancels, permissions denied)
            Console.WriteLine($"Error capturing photo: {ex.Message}");
        }

        return imageItem;
    }

    public async Task<ImageItem> CaptureItemPhotoAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var imageItem = new ImageItem();

        try
        {
            await CaptureAndSavePhoto(imageItem.FileName, Constants.PathToItemPhotos);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user cancels, permissions denied)
            Console.WriteLine($"Error capturing photo: {ex.Message}");
        }

        return imageItem;
    }

    private async Task CaptureAndSavePhoto(string imageFileName, string pathPrefix)
    {
        byte[] bytes = await CapturePhoto();
        await SavePhoto(imageFileName, pathPrefix, bytes);
    }

    private async Task SavePhoto(string imageFileName, string pathPrefix, byte[] bytes)
    {
        await fileHandler.SaveFileAsync(imageFileName, pathPrefix, bytes);
    }

    private async Task<byte[]> CapturePhoto()
    {
        byte[] bytes;
        FileResult? photo = await mediaPicker.PickPhotoAsync();
        if (photo != null)
        {
            using Stream stream = await photo.OpenReadAsync();
            bytes = new byte[stream.Length];
            await stream.ReadExactlyAsync(bytes, 0, (int)stream.Length);

            return bytes;
        }

        return Array.Empty<byte>();
    }
}
