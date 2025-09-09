using System.Globalization;

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
                // File URI (file://) → load from local file system
                if (uri.IsFile)
                {
                    // Prefer local path for file URIs
                    return ImageSource.FromFile(uri.LocalPath);
                }
                // Remote http/https → use FromUri with caching enabled
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

                // Any other absolute scheme – try URI image source directly
                return new UriImageSource { Uri = uri };
            }

            // Absolute file path (without URI scheme)
            if (Path.IsPathRooted(s))
            {
                return ImageSource.FromFile(s);
            }

            // App resource (bundled) – resolves by filename
            return ImageSource.FromFile(s);
        }
        catch
        {
            // Fallback placeholder (must exist in Resources/Images)
            const string fallback = "dotnet_bot.png";
            try { return ImageSource.FromFile(fallback); }
            catch { return null; }
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
