using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
        LanguageOptions =
        [
            L("System"),
            L("English"),
            L("Polish"),
            L("German (AI-Translated)"),
            L("Spanish (AI-Translated)"),
        ];
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

    public IReadOnlyList<string> LanguageOptions { get; }

    public string SelectedLanguageOption
    {
        get => applicationSettings.Language switch
        {
            LanguagePreference.English => L("English"),
            LanguagePreference.Polish => L("Polish"),
            LanguagePreference.German => L("German (AI-Translated)"),
            LanguagePreference.Spanish => L("Spanish (AI-Translated)"),
            _ => L("System"),
        };
        set
        {
			if (string.IsNullOrWhiteSpace(value))
			{
				return;
			}

            var language = value == L("English")
                ? LanguagePreference.English
                : value == L("Polish")
                    ? LanguagePreference.Polish
                    : value == L("German (AI-Translated)")
                        ? LanguagePreference.German
                        : value == L("Spanish (AI-Translated)")
                            ? LanguagePreference.Spanish
                            : LanguagePreference.System;

            if (applicationSettings.Language == language)
            {
                return;
            }

            applicationSettings.Language = language;
            OnPropertyChanged();
        }
    }

    private static string L(string key) => LocalizationManager.Current.Get(key);

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

    public bool IsWmsExperimentalMode
    {
        get => applicationSettings.IsWmsExperimentalMode;
        set
        {
            if (applicationSettings.IsWmsExperimentalMode == value)
            {
                return;
            }

            applicationSettings.IsWmsExperimentalMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWmsDisabled));
            OnPropertyChanged(nameof(IsAdvancedAppMode));
        }
    }

    public bool IsWmsDisabled => !IsWmsExperimentalMode;

    [RelayCommand]
    private void EnableWmsExperimentalMode() => IsWmsExperimentalMode = true;

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
    private void SelectSimpleAppMode()
        => IsAdvancedAppMode = false;

    [RelayCommand]
    private void SelectAdvancedAppMode()
        => IsAdvancedAppMode = true;
}
