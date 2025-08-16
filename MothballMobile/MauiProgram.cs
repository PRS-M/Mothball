using CoreApp.Services.Implementations;
using CoreApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Media;
using MothballMobile.Core.Services;
using MothballMobile.UI.ViewModels;

namespace MothballMobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Font Awesome 7 Free-Regular-400.otf", "FontAwesome");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
		ConfigureServices(builder.Services);

		return builder.Build();
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		// Register your services here
		services.AddSingleton<ICameraHandler, CameraHandler>();
		services.AddSingleton<IFileHandler, MobileFileHandler>();
		services.AddSingleton<JsonHandler>();
		services.AddSingleton<ContainerJsonHandler>();
		services.AddSingleton(typeof(IFileSystem), FileSystem.Current);
		services.AddSingleton(typeof(IMediaPicker), MediaPicker.Default);

		// services.AddTransient(typeof(IFileSystem), typeof(FileSystem));
		services.AddTransient<AddContainerViewModel>();
		services.AddTransient<ContainerListViewModel>();
	}
}
