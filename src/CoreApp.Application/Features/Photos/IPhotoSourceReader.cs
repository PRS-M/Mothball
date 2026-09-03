namespace CoreApp.Application.Features.Photos;

/// <summary>
/// Defines operations for reading bytes from a photo source.
/// </summary>
public interface IPhotoSourceReader
{
    /// <summary>
    /// Reads photo bytes from the selected source and reports resize progress.
    /// </summary>
    /// <param name="source">The value used by the operation.</param>
    /// <param name="resizeProgress">The value used by the operation.</param>
    Task<byte[]> GetPhotoBytesAsync(PhotoSource source, IProgress<double>? resizeProgress);
}
