using CoreApp.Utilities;

namespace CoreApp.Features.Photos;

/// <summary>
/// Defines operations for capturing and removing temporary photos.
/// </summary>
public interface ITemporaryPhotoService
{
    /// <summary>
    /// Captures a temporary photo from the selected source.
    /// </summary>
    /// <param name="resizeProgress">Optionally receives progress while the photo is resized.</param>
    /// <param name="source">The source from which to capture the photo.</param>
    Task<ImageService.TemporaryPhotoCapture?> CaptureTemporaryPhotoAsync(
        IProgress<double>? resizeProgress = null,
        PhotoSource source = PhotoSource.Library);

    /// <summary>
    /// Deletes a temporary photo file.
    /// </summary>
    /// <param name="fileName">The value used by the operation.</param>
    Task DeleteTemporaryPhotoAsync(string fileName);
}
