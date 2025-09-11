using Microsoft.Extensions.Logging;
using Microsoft.Maui.Media;
using MothballMobile.Infrastructure;
using MothballMobile.UI.ViewModels;
using MothballMobile.Infrastructure.DatabaseModels;
using CoreApp.Interfaces;
using CoreApp.Services;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Microsoft.Maui.Handlers;
using Infrastructure.Utilities;
#if IOS
using UIKit;
#endif

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
				// Solid face for Font Awesome icons
				fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FontAwesomeSolid");
			});
#if DEBUG
		builder.Logging.AddDebug();
#endif
		ConfigureServices(builder.Services);

		// Platform tweaks
		builder.ConfigureMauiHandlers(handlers =>
		{
#if IOS
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
			});
#endif
		});

		return builder.Build();
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		// Register your services here
		services.AddTransient<IDebouncer>(_ => new Debouncer(300));
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
		services.AddSingleton<IImagePathResolver, ImagePathResolver>();
#if DEBUG
		services.AddSingleton<DemoDataSeeder>();
#endif
		// Navigation abstraction
		services.AddSingleton<Infrastructure.INavigationService, Infrastructure.ShellNavigationService>();
		// Popup abstraction
		services.AddSingleton<Infrastructure.IPopupService, Infrastructure.MauiPopupService>();
		services.AddTransient<AddContainerViewModel>();
		services.AddTransient<ContainerListViewModel>();
		services.AddTransient<ItemsListViewModel>();
		services.AddTransient<ContainerDetailsViewModel>();
		services.AddTransient<ItemDetailsViewModel>();
		services.AddTransient<AddItemViewModel>();
	}
}
