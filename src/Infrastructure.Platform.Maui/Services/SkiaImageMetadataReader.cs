using CoreApp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Infrastructure.Services;

public sealed class SkiaImageMetadataReader : IImageMetadataReader
{
    private readonly ILogger<SkiaImageMetadataReader> logger;

    public SkiaImageMetadataReader(ILogger<SkiaImageMetadataReader> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

            // Cancellation is a control-flow signal and must not be converted into
            // the same "unreadable image" result used for malformed image files.
            catch (OperationCanceledException exception)
            {
                logger.LogWarning(exception, "Reading image metadata was cancelled for '{ImagePath}'.", filePath);
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Unable to read image metadata for '{ImagePath}'.", filePath);
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
