using System;

namespace CoreApp.Interfaces;

/// <summary>
/// Port for capturing a photo from the device camera/gallery.
/// Returns raw bytes; domain/application decides how to use them.
/// </summary>
public interface ICameraHandler
{
    /// <summary>
    /// Captures a photo and returns its bytes, or an empty array if canceled.
    /// </summary>
    Task<byte[]> CapturePhotoAsync();
}
