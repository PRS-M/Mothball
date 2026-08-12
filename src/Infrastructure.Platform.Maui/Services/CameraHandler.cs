using System;
using CoreApp.Interfaces;
using CoreApp.Utilities;
using SkiaSharp;

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
                using var managedStream = new SKManagedStream(sourceStream, disposeManagedStream: false);
                using var codec = SKCodec.Create(managedStream);
                if (codec is null)
                {
                    return null;
                }

                using var decoded = SKBitmap.Decode(codec);
                if (decoded is null)
                {
                    return null;
                }

                resizeProgress?.Report(0.45);
                using var oriented = ApplyExifOrientation(decoded, codec.EncodedOrigin);
                var targetSize = CalculateTargetSize(
                    oriented.Width,
                    oriented.Height,
                    Constants.PhotoThumbnailMaxWidthPx,
                    Constants.PhotoThumbnailMaxHeightPx);

                using var resized = ResizeBitmap(oriented, targetSize.Width, targetSize.Height);

                resizeProgress?.Report(0.75);
                using var image = SKImage.FromBitmap(resized);
                using var output = image.Encode(SKEncodedImageFormat.Jpeg, 80);
                if (output is null)
                {
                    return null;
                }

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

    private static (int Width, int Height) CalculateTargetSize(int sourceWidth, int sourceHeight, int maxWidth, int maxHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return (Math.Max(1, maxWidth), Math.Max(1, maxHeight));
        }

        double ratio = Math.Min((double)maxWidth / sourceWidth, (double)maxHeight / sourceHeight);
        ratio = Math.Min(1, ratio);

        int width = Math.Max(1, (int)Math.Round(sourceWidth * ratio));
        int height = Math.Max(1, (int)Math.Round(sourceHeight * ratio));
        return (width, height);
    }

    private static SKBitmap ResizeBitmap(SKBitmap source, int width, int height)
    {
        if (source.Width == width && source.Height == height)
        {
            return source.Copy();
        }

        var destination = new SKBitmap(width, height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(destination);
        using var paint = new SKPaint
        {
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true,
            IsDither = true
        };

        canvas.DrawBitmap(source, new SKRect(0, 0, width, height), paint);
        canvas.Flush();
        return destination;
    }

    private static SKBitmap ApplyExifOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        bool swapDimensions = origin is SKEncodedOrigin.LeftTop
            or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom
            or SKEncodedOrigin.LeftBottom;

        int width = swapDimensions ? source.Height : source.Width;
        int height = swapDimensions ? source.Width : source.Height;

        var destination = new SKBitmap(width, height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(destination);
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };

        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;

            case SKEncodedOrigin.BottomRight:
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;

            case SKEncodedOrigin.BottomLeft:
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;

            case SKEncodedOrigin.RightTop:
            case SKEncodedOrigin.LeftTop:
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;

            case SKEncodedOrigin.LeftBottom:
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(0, height);
                canvas.RotateDegrees(-90);
                break;
        }

        canvas.DrawBitmap(source, 0, 0, paint);
        canvas.Flush();
        return destination;
    }
}
