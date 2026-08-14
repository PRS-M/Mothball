using System.Globalization;
using Microsoft.Extensions.Logging;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.Converters;

public class PathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Ensure this converter is only used for ImageSource targets
        if (targetType != typeof(ImageSource) && !targetType.IsAssignableFrom(typeof(ImageSource)))
            return null;

        if (value is not string s || string.IsNullOrWhiteSpace(s))
            return null;

        try
        {
            // If value is an absolute URI or path
            if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
            {
                // File URI (file://) â load from local file system
                if (uri.IsFile)
                {
                    // Prefer local path for file URIs
                    return ImageSource.FromFile(uri.LocalPath);
                }
                // Remote http/https â use FromUri with caching enabled
                if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                    uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    return new UriImageSource
                    {
                        Uri = uri,
                        CachingEnabled = true,
                        CacheValidity = TimeSpan.FromDays(7)
                    };
                }

                // Any other absolute scheme â try URI image source directly
                return new UriImageSource { Uri = uri };
            }

            // Absolute file path (without URI scheme)
            if (Path.IsPathRooted(s))
            {
                return ImageSource.FromFile(s);
            }

            // App resource (bundled) â resolves by filename
            return ImageSource.FromFile(s);
        }
        catch (Exception ex)
        {
            MauiLogger.For<PathToImageSourceConverter>()
                ?.LogWarning(ex, "Image source conversion failed for {ImagePath}.", s);
            // Fallback placeholder (must exist in Resources/Images)
            const string fallback = "mothball_logo.png";
            try { return ImageSource.FromFile(fallback); }
            catch (Exception fallbackEx)
            {
                MauiLogger.For<PathToImageSourceConverter>()
                    ?.LogError(fallbackEx, "Fallback image source conversion failed for {FallbackImagePath}.", fallback);
                return null;
            }
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
