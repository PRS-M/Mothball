using System;

namespace CoreApp.Interfaces;

/// <summary>
/// Abstraction for capturing a photo from the device camera or gallery.
/// Returns raw bytes; domain/application logic decides how to use them.
/// </summary>
public interface ICameraHandler
{
    /// <summary>
    /// Captures a photo and returns its bytes, or an empty array if the operation is canceled.
    /// </summary>
    Task<byte[]> CapturePhotoAsync(IProgress<double>? resizeProgress = null);
}
