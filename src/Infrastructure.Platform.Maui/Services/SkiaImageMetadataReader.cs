using CoreApp.Domain.ValueObjects;
using SkiaSharp;

namespace Infrastructure.Services;

public sealed class SkiaImageMetadataReader : IImageMetadataReader
{
    /// <inheritdoc />
    public Task<ImageDimensions?> ReadDimensionsAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return Task.FromResult<ImageDimensions?>(null);

        var filePath = ResolveLocalFilePath(imagePath);
        if (filePath is null || !File.Exists(filePath))
            return Task.FromResult<ImageDimensions?>(null);

        return Task.Run<ImageDimensions?>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var stream = File.OpenRead(filePath);
                using var codec = SKCodec.Create(stream);
                var width = codec?.Info.Width ?? 0;
                var height = codec?.Info.Height ?? 0;

                if (width <= 0 || height <= 0)
                    return null;

                return new ImageDimensions(width, height);
            }
            catch
            {
                return null;
            }
        }, cancellationToken);
    }

    private static string? ResolveLocalFilePath(string imagePath)
    {
        if (Uri.TryCreate(imagePath, UriKind.Absolute, out var uri))
            return uri.IsFile ? uri.LocalPath : null;

        return Path.IsPathRooted(imagePath) ? imagePath : null;
    }
}
