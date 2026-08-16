using CoreApp.Entities.Shared;

namespace CoreApp.Abstractions.Platform;

public interface IImageMetadataReader
{
    Task<ImageDimensions?> ReadDimensionsAsync(string imagePath, CancellationToken cancellationToken = default);
}
