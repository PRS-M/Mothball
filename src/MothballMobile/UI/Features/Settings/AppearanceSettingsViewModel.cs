using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Contracts;
using Microsoft.Maui.Devices;

namespace MothballMobile.UI.Features.Settings;

/// <summary>
/// Handles theme mode, color theme, and advanced-mode preferences on the settings page.
/// </summary>
public partial class AppearanceSettingsViewModel : ObservableObject
{
    private readonly IApplicationSettings applicationSettings;

    public AppearanceSettingsViewModel(IApplicationSettings applicationSettings)
    {
        this.applicationSettings = applicationSettings;
    }

    public IReadOnlyList<string> ModeOptions { get; } =
    [
        "Auto (System)",
        "Light",
        "Dark",
    ];

    public IReadOnlyList<string> ThemeOptions { get; } =
    [
        "Olive Workshop",
        "Blueprint Ledger",
        "Terracotta Archive",
        "Saffron Utility",
        "Coastal Inventory",
        "Berry Archive",
    ];

    public string SelectedModeOption
    {
        get => applicationSettings.ThemeOverride switch
        {
            AppTheme.Light => "Light",
            AppTheme.Dark => "Dark",
            _ => "Auto (System)",
        };
        set
        {
            var theme = value switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified,
            };

            if (applicationSettings.ThemeOverride == theme)
            {
                return;
            }

            applicationSettings.ThemeOverride = theme;
            Application.Current!.UserAppTheme = theme;
            OnPropertyChanged();
        }
    }

    public string SelectedThemeOption
    {
        get => applicationSettings.ThemePalette switch
        {
            ThemePalette.BlueprintLedger => "Blueprint Ledger",
            ThemePalette.TerracottaArchive => "Terracotta Archive",
            ThemePalette.SaffronUtility => "Saffron Utility",
            ThemePalette.CoastalInventory => "Coastal Inventory",
            ThemePalette.BerryArchive => "Berry Archive",
            _ => "Olive Workshop",
        };
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            ThemePalette? palette = value switch
            {
                "Olive Workshop" => ThemePalette.OliveWorkshop,
                "Blueprint Ledger" => ThemePalette.BlueprintLedger,
                "Terracotta Archive" => ThemePalette.TerracottaArchive,
                "Saffron Utility" => ThemePalette.SaffronUtility,
                "Coastal Inventory" => ThemePalette.CoastalInventory,
                "Berry Archive" => ThemePalette.BerryArchive,
                _ => null,
            };

            if (palette is null || applicationSettings.ThemePalette == palette.Value)
            {
                return;
            }

            applicationSettings.ThemePalette = palette.Value;
            OnPropertyChanged();
        }
    }

    public bool IsAdvancedAppMode
    {
        get => applicationSettings.IsAdvancedMode;
        set
        {
            if (applicationSettings.IsAdvancedMode == value)
            {
                return;
            }

            applicationSettings.IsAdvancedMode = value;
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private void SelectSimpleAppMode()
        => IsAdvancedAppMode = false;

    [RelayCommand]
    private void SelectAdvancedAppMode()
        => IsAdvancedAppMode = true;
}
