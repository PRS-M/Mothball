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

    public async Task<PhotoWithData> CapturePhotoAsync(Item item, string containerName)
    {
        string fileName = $"{item.Name}-{Guid.NewGuid()}.jpg";

        PhotoWithData photoWithData = new PhotoWithData
        {
            FileName = fileName,
        };

        try
        {
            FileResult? photo = await mediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                using Stream stream = await photo.OpenReadAsync();
                byte[] bytes = new byte[stream.Length];

                string path = Path.Combine(Constants.PathToPhotos, containerName);
                await stream.ReadExactlyAsync(bytes, 0, (int)stream.Length);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                await fileHandler.SaveFileAsync(fileName, path, bytes);
                photoWithData.ImageData = bytes;
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user cancels, permissions denied)
            Console.WriteLine($"Error capturing photo: {ex.Message}");
        }

        return photoWithData;
    }
}
