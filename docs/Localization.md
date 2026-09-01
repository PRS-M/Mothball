# Localization

Mothball supports English, Polish, German, and Spanish. English is the neutral/fallback language; Polish is reviewed, while German and Spanish are explicitly marked `(AI-Translated)` in the language selector and resource files until they receive native-speaker review.

The implementation follows the [.NET MAUI localization guidance](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/localization?view=net-maui-10.0): UI text is stored in `.resx` files and XAML reads generated `AppResources` properties through `x:Static`.

## Resource files

| File | Purpose |
| --- | --- |
| `src/MothballMobile/Resources/Localization/AppResources.resx` | Default English values and the generated `AppResources` class. |
| `src/MothballMobile/Resources/Localization/AppResources.pl.resx` | Reviewed Polish values with the same resource keys. |
| `src/MothballMobile/Resources/Localization/AppResources.de.resx` | German AI-translated values; pending native-speaker review. |
| `src/MothballMobile/Resources/Localization/AppResources.es.resx` | Spanish AI-translated values; pending native-speaker review. |
| `src/MothballMobile/Resources/Localization/ResourceKeyMap.cs` | Maps legacy English literals used by C# presentation code to resource keys. |

The default resource file is configured for strongly typed code generation in `MothballMobile.csproj`. `<NeutralLanguage>en</NeutralLanguage>` ensures a readable English fallback when no matching translation exists.

Use generated resources in XAML:

```xml
xmlns:strings="clr-namespace:MothballMobile.Resources.Localization"

<Button Text="{x:Static strings:AppResources.Save}" />
```

For presentation code, use the configured localization service:

```csharp
LocalizationManager.Current.Get("Save");
LocalizationManager.Current.Format("Deleted: {0}", fileName);
```

When adding a user-facing string, add the same named key to every `.resx` file. If C# uses the string through `LocalizationManager.Current`, also add its literal-to-key entry to `ResourceKeyMap.cs`. German and Spanish values are AI-generated and must retain that status until native-speaker review.

## Language preference and startup

`IApplicationSettings.Language` persists one of these values:

- `System`
- `English`
- `Polish`
- `German` (`AI-Translated`)
- `Spanish` (`AI-Translated`)

`LocalizationService.SetLanguage` resolves the preference and sets both `CultureInfo.DefaultThreadCurrentCulture` / `DefaultThreadCurrentUICulture` and the current-thread equivalents. `App` applies the persisted value before `InitializeComponent()`, so generated resource properties use the selected culture while XAML is first created.

Platform declarations are also required for native controls:

- `Platforms/iOS/Info.plist` and `Platforms/MacCatalyst/Info.plist` declare `en`, `pl`, `de`, and `es` through `CFBundleLocalizations`, with `en` as `CFBundleDevelopmentRegion`.
- `Platforms/Windows/Package.appxmanifest` declares `en-US`, `pl-PL`, `de-DE`, and `es-ES` resources.

## Changing language while the app is running

The MAUI documentation shows `x:Static` access and states that generated resource properties resolve from `CurrentUICulture`; it does not prescribe live UI updates after a culture change. Existing XAML controls therefore retain their text until they are created again.

Mothball deliberately does **not** replace `Window.Page`, rebuild `AppShell`, change the process culture, or display a modal dialog when the user changes language. Replacing the root page or showing another native modal while iOS is completing the `Picker` interaction can freeze the app and also discards navigation state. The Picker only saves the preference. Settings displays a localized restart notice beneath it, and the language is fully applied when the user reopens the app.

This is also consistent with a longstanding MAUI localization report: runtime culture changes with Shell can leave UI partially translated, while a new app launch gives consistent text. See [.NET MAUI issue #5595](https://github.com/dotnet/maui/issues/5595).

Do not attempt to programmatically terminate or relaunch the application, particularly on iOS. Keep the restart notice in Settings and let the user reopen the app.

## Verification checklist

1. Start with the device language set to English; verify English UI.
2. Start with the device language set to Polish, German, or Spanish and select `System`; verify the matching UI from the first screen.
3. Choose any language in Settings; verify the restart notice is visible and the app remains responsive.
4. Close and reopen Mothball; verify navigation labels, page titles, controls, popups, validation, and formatted quantities use the selected language.
5. Check iOS/Mac Catalyst and Windows packaging declarations when adding another supported language.
