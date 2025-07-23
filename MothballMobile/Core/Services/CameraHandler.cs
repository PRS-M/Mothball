using System;
using CoreApp;

namespace MothballMobile.Core.Services;

public class CameraHandler
{
    private IMediaPicker mediaPicker;
    private MobileFileSystemHandler fileHandler;

    public CameraHandler(IMediaPicker mediaPicker, MobileFileSystemHandler fileHandler)
    {
        this.mediaPicker = mediaPicker ?? throw new ArgumentNullException(nameof(mediaPicker));
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
    }

    public async Task CapturePhotoAsync(Item item, string containerName)
    {
        try
        {
            FileResult? photo = await mediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                Stream stream = await photo.OpenReadAsync();
                byte[] bytes = new byte[stream.Length];

                await stream.ReadExactlyAsync(bytes, 0, (int)stream.Length);
                await fileHandler.SaveFileAsync($"{item.Name}-{Guid.NewGuid()}.jpg", containerName, bytes);
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user cancels, permissions denied)
            Console.WriteLine($"Error capturing photo: {ex.Message}");
        }
    }
}
