using CoreApp.Entities.Shared;

namespace CoreApp.Interfaces;

public interface IImageMetadataReader
{
    Task<ImageDimensions?> ReadDimensionsAsync(string imagePath, CancellationToken cancellationToken = default);
}
