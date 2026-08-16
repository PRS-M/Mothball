using System;

namespace CoreApp.Abstractions.Platform;

public enum PhotoSource
{
    Library,
    Camera
}

/// <summary>
/// Abstraction for capturing a photo from the device camera or gallery.
/// Returns raw bytes; domain/application logic decides how to use them.
/// </summary>
public interface ICameraHandler
{
    /// <summary>
    /// Selects a photo from the device photo library and returns its bytes, or an empty array if the operation is canceled.
    /// </summary>
    Task<byte[]> SelectPhotoAsync(IProgress<double>? resizeProgress = null);

    /// <summary>
    /// Captures a new photo from the device camera and returns its bytes, or an empty array if the operation is canceled.
    /// </summary>
    Task<byte[]> CapturePhotoAsync(IProgress<double>? resizeProgress = null);
}
