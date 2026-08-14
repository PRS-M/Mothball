using CoreApp.Services;
using CoreApp.Utilities;

namespace CoreApp.Interfaces;

public interface ITemporaryPhotoService
{
    Task<ImageService.TemporaryPhotoCapture?> CaptureTemporaryPhotoAsync(
        IProgress<double>? resizeProgress = null,
        PhotoSource source = PhotoSource.Library);

    Task DeleteTemporaryPhotoAsync(string fileName);
}
