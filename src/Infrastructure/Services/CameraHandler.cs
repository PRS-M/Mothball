using System;
using CoreApp;
using CoreApp.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Mobile implementation of camera functionality using MAUI's media picker.
/// </summary>
public class CameraHandler : ICameraHandler
{
    private readonly IMediaPicker mediaPicker;

    public CameraHandler(IMediaPicker mediaPicker)
    {
        this.mediaPicker = mediaPicker ?? throw new ArgumentNullException(nameof(mediaPicker));
    }

    public async Task<byte[]> CapturePhotoAsync()
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
