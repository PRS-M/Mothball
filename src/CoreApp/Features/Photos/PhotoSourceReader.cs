using CoreApp.Utilities;

namespace CoreApp.Features.Photos;

public sealed class PhotoSourceReader : IPhotoSourceReader
{
    private readonly ICameraHandler cameraHandler;

    public PhotoSourceReader(ICameraHandler cameraHandler)
    {
        this.cameraHandler = cameraHandler ?? throw new ArgumentNullException(nameof(cameraHandler));
    }

    public Task<byte[]> GetPhotoBytesAsync(PhotoSource source, IProgress<double>? resizeProgress)
        => source switch
        {
            PhotoSource.Camera => cameraHandler.CapturePhotoAsync(resizeProgress),
            PhotoSource.Library => cameraHandler.SelectPhotoAsync(resizeProgress),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown photo source.")
        };
}
