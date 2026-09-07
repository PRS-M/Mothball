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
        INavigationService nav)
    {
        Appearance = appearance;
        Backup = backup;
        this.nav = nav;
    }

    public AppearanceSettingsViewModel Appearance { get; }

    public BackupSettingsViewModel Backup { get; }

    [RelayCommand]
    private Task NavigateToAdvancedSettingsAsync()
        => nav.GoToAsync(NavigationRoutes.AdvancedSettings);
}
