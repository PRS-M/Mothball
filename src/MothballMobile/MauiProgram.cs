using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MothballMobile.Composition;
using Microsoft.Maui.Handlers;
#if IOS || ANDROID
using Plugin.AdMob;
using Plugin.AdMob.Configuration;
#endif

#if IOS || MACCATALYST
using UIKit;
#endif

#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace MothballMobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		var backendOverride = Environment.GetEnvironmentVariable("MOTHBALL_PERSISTENCE_BACKEND");
		builder.Configuration
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				[PersistenceConfiguration.BackendKey] =
					string.IsNullOrWhiteSpace(backendOverride)
						? PersistenceConfiguration.SqliteBackend
						: backendOverride
			});

		builder.UseMauiApp<App>();
#if IOS || ANDROID
		builder.UseAdMob();
#endif
		builder.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Font Awesome 7 Free-Regular-400.otf", "FontAwesome");
				// Solid face for Font Awesome icons
				fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FontAwesomeSolid");
			});
#if MAUI_DEVFLOW
		builder.AddMauiDevFlowAgent();
#endif
#if IOS || ANDROID
		AdConfig.UseTestAdUnitIds = true;
		AdConfig.DisableConsentCheck = true;
#endif
#if DEBUG
		builder.Logging.AddDebug();
#endif
		builder.Services
			.AddCoreApplication()
			.AddPersistence(builder.Configuration)
			.AddPlatformServices()
			.AddViewModels();

		// Platform tweaks
		builder.ConfigureMauiHandlers(ConfigurePlatformHandlers);

		return builder.Build();
	}

	private static void ConfigurePlatformHandlers(IMauiHandlersCollection handlers)
	{
		#if IOS || MACCATALYST
		SearchBarHandler.Mapper.AppendToMapping("ContrastBackground", (handler, view) =>
		{
			var sb = handler.PlatformView;
			if (sb is null) return;
			sb.SearchBarStyle = UISearchBarStyle.Minimal;
			sb.BackgroundImage = new UIImage();
			sb.Layer.BorderWidth = 0;

			// On iOS/MacCatalyst, SearchBar rendering is owned by UISearchTextField,
			// so set contrast colors directly on the native field.
			try
			{
				var tf = sb.SearchTextField;
				if (tf is not null)
				{
					var isDark = sb.TraitCollection?.UserInterfaceStyle == UIUserInterfaceStyle.Dark;
					tf.BackgroundColor = isDark
						? UIColor.FromRGB(74, 68, 88)     // SecondaryContainerDark
						: UIColor.FromRGB(232, 222, 248); // SecondaryContainer
					tf.TextColor = isDark
						? UIColor.FromRGB(232, 222, 248)   // OnSecondaryContainerDark
						: UIColor.FromRGB(29, 25, 43);      // OnSecondaryContainer
					tf.BorderStyle = UITextBorderStyle.RoundedRect;
					tf.Layer.BorderWidth = 0;
					tf.Layer.CornerRadius = 10;
					tf.Layer.ShadowOpacity = 0;
					tf.Layer.MasksToBounds = true;
				}
			}
			catch (Exception ex)
			{
				MauiLogger.For("MothballMobile.PlatformHandlers")
					?.LogWarning(ex, "SearchBar platform styling failed.");
				// Best-effort platform polish only.
			}
		});
		#endif
	}
}
