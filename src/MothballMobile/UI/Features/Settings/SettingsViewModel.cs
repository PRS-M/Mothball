using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MothballMobile.UI.Features.Settings;

/// <summary>
/// Composes the settings page from its independent appearance, backup, and signing-key sections.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly INavigationService nav;

    public SettingsViewModel(
        AppearanceSettingsViewModel appearance,
        BackupSettingsViewModel backup,
        BackupSigningKeySettingsViewModel signingKey,
        INavigationService nav)
    {
        Appearance = appearance;
        Backup = backup;
        SigningKey = signingKey;
        this.nav = nav;
    }

    public AppearanceSettingsViewModel Appearance { get; }

    public BackupSettingsViewModel Backup { get; }

    public BackupSigningKeySettingsViewModel SigningKey { get; }

    [RelayCommand]
    private Task NavigateToBackgroundOperationsAsync()
        => nav.GoToAsync(NavigationRoutes.BackgroundOperations);

    [RelayCommand]
    private Task NavigateToWmsAsync()
        => nav.GoToAsync(NavigationRoutes.WmsHome);
}
