using CoreApp.Entities.Shared;

namespace CoreApp.Abstractions.Platform;

/// <summary>
/// Defines operations for reading image metadata.
/// </summary>
public interface IImageMetadataReader
{
    /// <summary>
    /// Reads the dimensions of an image file.
    /// </summary>
    /// <param name="imagePath">The value used by the operation.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task<ImageDimensions?> ReadDimensionsAsync(string imagePath, CancellationToken cancellationToken = default);
}
