using System.Globalization;
using System.Resources;

namespace MothballMobile.Resources.Localization;

internal static class AppResources
{
    public static ResourceManager ResourceManager { get; } = new EmptyResourceManager();

    private sealed class EmptyResourceManager : ResourceManager
    {
        public override string? GetString(string name, CultureInfo? culture)
            => null;
    }
}
