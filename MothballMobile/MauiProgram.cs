using Microsoft.Extensions.Logging;
using Microsoft.Maui.Media;
using MothballMobile.Infrastructure;
using MothballMobile.UI.ViewModels;
using MothballMobile.Infrastructure.DatabaseModels;
using CoreApp.Interfaces;
using CoreApp.Services;
using Infrastructure.Interfaces;
using Infrastructure.Services;

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
	services.AddSingleton<InventoryJsonHandler>();
		services.AddSingleton(typeof(IFileSystem), FileSystem.Current);
		services.AddSingleton(typeof(IMediaPicker), MediaPicker.Default);

		// Database and repositories
		services.AddSingleton<MothballDatabase>();
		services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));
		services.AddSingleton<IInventoryDomainRepository, InventoryDomainRepository>();
	services.AddTransient<AddContainerViewModel>();
	services.AddTransient<ContainerListViewModel>();
	}
}
