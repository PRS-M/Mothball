using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MothballMobile.Composition;
using Microsoft.Maui.Handlers;

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

		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
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
		SearchBarHandler.Mapper.AppendToMapping("TransparentBackground", (handler, view) =>
		{
			var sb = handler.PlatformView;
			if (sb is null) return;
			sb.SearchBarStyle = UISearchBarStyle.Minimal;
			sb.BackgroundColor = UIColor.Clear;
			sb.BarTintColor = UIColor.Clear;
			sb.BackgroundImage = new UIImage();
			sb.Layer.BackgroundColor = UIColor.Clear.CGColor;
			sb.Layer.BorderWidth = 0;

			// Remove the inner SearchTextField styling that can show up as
			// top/bottom hairlines when the SearchBar sits inside a rounded Border.
			// (Available on iOS 13+/MacCatalyst.)
			try
			{
				// Clears the native search field "plate" background.
				sb.SetSearchFieldBackgroundImage(new UIImage(), UIControlState.Normal);

				var tf = sb.SearchTextField;
				if (tf is not null)
				{
					tf.BackgroundColor = UIColor.Clear;
					tf.Background = null;
					tf.BorderStyle = UITextBorderStyle.None;
					tf.Layer.BorderWidth = 0;
					tf.Layer.ShadowOpacity = 0;
					tf.Layer.MasksToBounds = true;
				}
			}
			catch
			{
				// Best-effort platform polish only.
			}
		});
		#endif
	}
}
