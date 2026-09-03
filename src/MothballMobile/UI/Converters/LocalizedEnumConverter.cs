using System.Globalization;

namespace MothballMobile.UI.Converters;

public sealed class LocalizedEnumConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Enum enumValue
            ? LocalizationManager.Current.Get(enumValue.ToString())
            : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
