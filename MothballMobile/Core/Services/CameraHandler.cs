using System;
using CoreApp;
using CoreApp.Models;
using CoreApp.Services.Interfaces;
using CoreApp.Utilities;

namespace MothballMobile.Core.Services;

public class CameraHandler : ICameraHandler
{
    private readonly IMediaPicker mediaPicker;
    private readonly IFileHandler fileHandler;

    public CameraHandler(IMediaPicker mediaPicker, IFileHandler fileHandler)
    {
        this.mediaPicker = mediaPicker ?? throw new ArgumentNullException(nameof(mediaPicker));
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
    }

    public async Task<Photo> CaptureContainerPhotoAsync(Container container)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container), "Container cannot be null.");
        }

        var photoWithData = new Photo
        {
            FileName = $"{Guid.NewGuid()}.jpg"
        };

        try
        {
            await CaptureAndSavePhoto(container.UniqueId, photoWithData.FileName, photoWithData);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user cancels, permissions denied)
            Console.WriteLine($"Error capturing photo: {ex.Message}");
        }

        return photoWithData;
    }

    public async Task<Photo> CaptureItemPhotoAsync(Item item, string containerId)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (string.IsNullOrEmpty(containerId))
            throw new ArgumentNullException(nameof(containerId));

        var photoWithData = new Photo
        {
            FileName = $"{Guid.NewGuid()}.jpg"
        };

        try
        {
            await CaptureAndSavePhoto(containerId, photoWithData.FileName, photoWithData);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user cancels, permissions denied)
            Console.WriteLine($"Error capturing photo: {ex.Message}");
        }

        return photoWithData;
    }

    private async Task CaptureAndSavePhoto(string containerId, string fileName, Photo photoWithData)
    {
        FileResult? photo = await mediaPicker.PickPhotoAsync();
        if (photo != null)
        {
            using Stream stream = await photo.OpenReadAsync();
            byte[] bytes = new byte[stream.Length];

            string path = Path.Combine(Constants.PathToItemPhotos, containerId);
            await stream.ReadExactlyAsync(bytes, 0, (int)stream.Length);

            await fileHandler.SaveFileAsync(fileName, path, bytes);
            photoWithData.ImageData = bytes;
        }
    }
}
