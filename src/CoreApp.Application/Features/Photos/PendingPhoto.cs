namespace CoreApp.Application.Features.Photos;

/// <summary>
/// Owns a photo captured but not yet attached to a persisted entity. Replacing the capture deletes the
/// previous temp file, and every terminal outcome (save or cancel) must discard it exactly once so no
/// orphaned temp file is left behind.
/// </summary>
public sealed class PendingPhoto
{
    private readonly ImageService imageService;
    private ImageService.TemporaryPhotoCapture? capture;

    public PendingPhoto(ImageService imageService)
    {
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }

    public bool HasPhoto => capture is not null;
    public string? FullPath => capture?.FullPath;
    public byte[]? Bytes => capture?.Bytes;

    /// <summary>
    /// Captures a new photo from the given source, discarding any previously staged capture first.
    /// </summary>
    /// <returns><see langword="true"/> when a photo was captured; <see langword="false"/> when the user canceled.</returns>
    public async Task<bool> CaptureAsync(PhotoSource source, IProgress<double>? resizeProgress = null)
    {
        var selected = await imageService.CaptureTemporaryPhotoAsync(resizeProgress, source);
        if (selected is null)
        {
            return false;
        }

        await DiscardAsync();
        capture = selected;
        return true;
    }

    /// <summary>
    /// Deletes the staged temp file, if any, and clears the pending state.
    /// </summary>
    public async Task DiscardAsync()
    {
        if (capture is null)
        {
            return;
        }

        var fileName = capture.FileName;
        capture = null;
        await imageService.DeleteTemporaryPhotoAsync(fileName);
    }
}
