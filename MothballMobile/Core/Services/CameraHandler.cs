using System;
using CoreApp;
using CoreApp.Services.Interfaces;
using CoreApp.Utilities;

namespace MothballMobile.Core.Services;

public class CameraHandler : ICameraHandler
{
    private readonly IMediaPicker mediaPicker;
    private readonly MobileFileSystemHandler fileHandler;

    public CameraHandler(IMediaPicker mediaPicker, MobileFileSystemHandler fileHandler)
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
            FileName = $"{container.Name}-{Guid.NewGuid()}.jpg"
        };

        try
        {
            await CaptureAndSavePhoto(container.Name, photoWithData.FileName, photoWithData);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user cancels, permissions denied)
            Console.WriteLine($"Error capturing photo: {ex.Message}");
        }

        return photoWithData;
    }

    public async Task<Photo> CaptureItemPhotoAsync(Item item, string containerName)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item), "Item cannot be null.");
        }

        var photoWithData = new Photo
        {
            FileName = $"{item.Name}-{Guid.NewGuid()}.jpg"
        };

        try
        {
            await CaptureAndSavePhoto(containerName, photoWithData.FileName, photoWithData);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user cancels, permissions denied)
            Console.WriteLine($"Error capturing photo: {ex.Message}");
        }

        return photoWithData;
    }

    private async Task CaptureAndSavePhoto(string containerName, string fileName, Photo photoWithData)
    {
        FileResult? photo = await mediaPicker.PickPhotoAsync();
        if (photo != null)
        {
            using Stream stream = await photo.OpenReadAsync();
            byte[] bytes = new byte[stream.Length];

            string path = Path.Combine(Constants.PathToPhotos, containerName);
            await stream.ReadExactlyAsync(bytes, 0, (int)stream.Length);

            await fileHandler.SaveFileAsync(fileName, path, bytes);
            photoWithData.ImageData = bytes;
        }
    }
}
