using CoreApp.Interfaces;
using CoreApp.Utilities;

namespace CoreApp.Features.Photos;

public interface ITemporaryPhotoService
{
    Task<ImageService.TemporaryPhotoCapture?> CaptureTemporaryPhotoAsync(
        IProgress<double>? resizeProgress = null,
        PhotoSource source = PhotoSource.Library);

    Task DeleteTemporaryPhotoAsync(string fileName);
}
