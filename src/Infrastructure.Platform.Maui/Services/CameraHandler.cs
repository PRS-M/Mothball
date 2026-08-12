using System;
using CoreApp.Interfaces;
using CoreApp.Utilities;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
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

    public async Task<byte[]> CapturePhotoAsync(IProgress<double>? resizeProgress = null)
    {
        var photos = await mediaPicker.PickPhotosAsync();
        FileResult? photo = photos?.FirstOrDefault();
        if (photo == null) return Array.Empty<byte>();

        resizeProgress?.Report(0.05);

        // Fast path: resize directly from stream to avoid allocating full-resolution bytes first.
        using (Stream resizeStream = await photo.OpenReadAsync())
        {
            byte[]? thumbnailBytes = await TryCreateThumbnailJpegAsync(resizeStream, resizeProgress);
            if (thumbnailBytes is { Length: > 0 })
            {
                resizeProgress?.Report(1);
                return thumbnailBytes;
            }
        }

        // Fallback path: if resizing fails, return the original bytes.
        resizeProgress?.Report(0.85);
        using Stream originalStream = await photo.OpenReadAsync();
        byte[] bytes = await ReadAllBytesAsync(originalStream);
        resizeProgress?.Report(1);
        return bytes;
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        // If the stream is seekable and the length is known, read directly into a pre-sized buffer.
        if (stream.CanSeek)
        {
            long length = stream.Length;
            if (length <= 0) return Array.Empty<byte>();
            if (length > int.MaxValue)
            {
                using var oversized = new MemoryStream();
                await stream.CopyToAsync(oversized);
                return oversized.ToArray();
            }

            byte[] buffer = new byte[length];
            await stream.ReadExactlyAsync(buffer, 0, (int)length);
            return buffer;
        }

        // Fallback for non-seekable streams.
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    private static async Task<byte[]?> TryCreateThumbnailJpegAsync(Stream sourceStream, IProgress<double>? resizeProgress)
    {
        // Convert the picked image into a stored thumbnail to reduce storage.
        try
        {
            return await Task.Run(() =>
            {
                resizeProgress?.Report(0.2);
                using SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(sourceStream);

                resizeProgress?.Report(0.45);
                image.Mutate(ctx =>
                {
                    ctx.AutoOrient();
                    ctx.Resize(new ResizeOptions
                    {
                        Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max,
                        Sampler = KnownResamplers.Triangle,
                        Size = new SixLabors.ImageSharp.Size(Constants.PhotoThumbnailMaxWidthPx, Constants.PhotoThumbnailMaxHeightPx)
                    });
                });

                resizeProgress?.Report(0.75);
                using var output = new MemoryStream();
                var encoder = new JpegEncoder { Quality = 80 };
                image.Save(output, encoder);
                resizeProgress?.Report(0.95);
                return output.ToArray();
            });
        }
        catch
        {
            // If thumbnail generation fails for any reason, fall back to the original bytes.
            return null;
        }
    }
}
