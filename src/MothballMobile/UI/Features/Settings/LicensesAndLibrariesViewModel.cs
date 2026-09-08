using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;

namespace MothballMobile.UI.Features.Settings;

/// <summary>
/// Presents the direct open-source libraries used by the application and their attribution links.
/// </summary>
public sealed partial class LicensesAndLibrariesViewModel : ObservableObject
{
    /// <summary>Creates the library attribution catalogue.</summary>
    public LicensesAndLibrariesViewModel()
    {
        Libraries =
        [
            new("CommunityToolkit.Mvvm", "8.4.0", "MIT", "https://github.com/CommunityToolkit/dotnet", "https://github.com/CommunityToolkit/dotnet/blob/main/License.md"),
            new("Plugin.AdMob", "10.0.90", "MIT", "https://github.com/marius-bughiu/Plugin.AdMob", "https://github.com/marius-bughiu/Plugin.AdMob/blob/main/LICENSE"),
            new("SkiaSharp", "3.119.2", "MIT", "https://github.com/mono/SkiaSharp", "https://github.com/mono/SkiaSharp/blob/main/LICENSE.md"),
            new("sqlite-net-pcl", "1.9.172", "MIT", "https://github.com/cjgaliana/SQLite.Net-PCL", "https://github.com/cjgaliana/SQLite.Net-PCL/blob/master/LICENSE"),
            new("SQLitePCLRaw", "2.1.12", "Apache-2.0", "https://github.com/ericsink/SQLitePCL.raw", "https://github.com/ericsink/SQLitePCL.raw/blob/main/LICENSE.txt"),
            new("ZXing.Net.Maui", "0.10.4", "MIT", "https://github.com/Redth/ZXing.Net.Maui", "https://github.com/Redth/ZXing.Net.Maui/blob/master/LICENSE"),
        ];
    }

    /// <summary>Gets the direct libraries used by the application.</summary>
    public IReadOnlyList<LibraryLicenseEntry> Libraries { get; }
}

/// <summary>Describes one library and its source and license links.</summary>
public sealed partial class LibraryLicenseEntry : ObservableObject
{
    /// <summary>Creates a library attribution entry.</summary>
    public LibraryLicenseEntry(string name, string version, string license, string githubUrl, string licenseUrl)
    {
        Name = name;
        Version = version;
        License = license;
        GitHubUrl = githubUrl;
        LicenseUrl = licenseUrl;
    }

    public string Name { get; }
    public string Version { get; }
    public string License { get; }
    public string GitHubUrl { get; }
    public string LicenseUrl { get; }

    [RelayCommand]
    private Task OpenGitHubAsync() => Launcher.Default.OpenAsync(new Uri(GitHubUrl));

    [RelayCommand]
    private Task OpenLicenseAsync() => Launcher.Default.OpenAsync(new Uri(LicenseUrl));
}
