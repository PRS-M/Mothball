using System.Globalization;
using Microsoft.Maui.Controls;

namespace MothballMobile.UI.Converters;

public class PathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
            return null;

        try
        {
            // For absolute file paths, load from file system
            if (System.IO.Path.IsPathRooted(s) || s.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                // Uri scheme or absolute path
                if (s.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    return new UriImageSource { Uri = new Uri(s) };
                }

                return ImageSource.FromFile(s);
            }

            // For app resources (e.g., dotnet_bot.png), use FromFile to resolve bundled resource
            return ImageSource.FromFile(s);
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
