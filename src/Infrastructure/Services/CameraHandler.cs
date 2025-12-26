using System;
using CoreApp;
using CoreApp.Interfaces;
using CoreApp.Utilities;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

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
        byte[] originalBytes;

        if (stream.CanSeek)
        {
            long length = stream.Length;
            if (length <= 0) return Array.Empty<byte>();

            byte[] buffer = new byte[length];
            await stream.ReadExactlyAsync(buffer, 0, (int)length);

            originalBytes = buffer;
        }
        else
        {
            // Fallback for non-seekable streams.
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            originalBytes = ms.ToArray();
        }

        if (originalBytes.Length == 0) return Array.Empty<byte>();

        // Convert the picked image into a stored thumbnail to reduce storage.
        try
        {
            using SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(originalBytes);
            image.Mutate(ctx =>
            {
                ctx.AutoOrient();
                ctx.Resize(new ResizeOptions
                {
                    Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max,
                    Size = new SixLabors.ImageSharp.Size(Constants.PhotoThumbnailMaxWidthPx, Constants.PhotoThumbnailMaxHeightPx)
                });
            });

            using var output = new MemoryStream();
            var encoder = new JpegEncoder { Quality = 85 };
            await image.SaveAsync(output, encoder);
            return output.ToArray();
        }
        catch
        {
            // If thumbnail generation fails for any reason, fall back to the original bytes.
            return originalBytes;
        }
    }
}
