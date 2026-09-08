using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MothballMobile.Infrastructure.Presentation.Popups;

namespace MothballMobile.UI.Features.Settings;

/// <summary>
/// Provides power-user settings and navigation to photo-processing history.
/// </summary>
public partial class AdvancedSettingsViewModel : ObservableObject
{
    private readonly IApplicationSettings applicationSettings;
    private readonly INavigationService navigation;
    public AdvancedSettingsViewModel(
        IApplicationSettings applicationSettings,
        BackupSigningKeySettingsViewModel signingKey,
        INavigationService navigation)
    {
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
        SigningKey = signingKey ?? throw new ArgumentNullException(nameof(signingKey));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    public BackupSigningKeySettingsViewModel SigningKey { get; }

    public bool IsBarcodeExtendedMode
    {
        get => applicationSettings.IsBarcodeExtendedMode;
        set
        {
            if (applicationSettings.IsBarcodeExtendedMode == value)
            {
                return;
            }

            applicationSettings.IsBarcodeExtendedMode = value;
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private Task NavigateToBackgroundOperationsAsync()
        => navigation.GoToAsync(NavigationRoutes.BackgroundOperations);

}
