using System.Runtime.InteropServices;

namespace CoreApp.Services;

public static class PlatformDetector
{
    public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

#if ANDROID || IOS || MACCATALYST || WINDOWS
    // MAUI-specific platform detection (using Microsoft.Maui.Devices)
    public static string MauiPlatform => Microsoft.Maui.Devices.DeviceInfo.Platform.ToString();

    public static bool IsAndroid() => Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android;
    public static bool IsIOS() => Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.iOS;
    public static bool IsMacCatalyst() => Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.MacCatalyst;
#endif

    public static string GetOS()
    {
#if ANDROID || IOS || MACCATALYST || WINDOWS
        return MauiPlatform;
#else
        if (IsWindows()) return "Windows";
        if (IsLinux()) return "Linux";
        if (IsMacOS()) return "macOS";
        return "Unknown";
#endif
    }
}
