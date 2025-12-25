using Microsoft.Extensions.Logging;
using Microsoft.Maui.Media;
using MothballMobile.Infrastructure;
using MothballMobile.UI.ViewModels;
using Infrastructure.Services;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;
using CoreApp.Interfaces;
using CoreApp.Services;
using Infrastructure.Interfaces;
using Microsoft.Maui.Handlers;
#if IOS || MACCATALYST
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
		});

		return builder.Build();
	}

	private static void ConfigureServices(IServiceCollection services)
    {
        RegisterServices(services);
        RegisterDatabase(services);
        RegisterViewModels(services);
    }

    private static void RegisterServices(IServiceCollection services)
	{
		// Register your services here
		services.AddTransient<IDebouncer>(_ => new Debouncer(300));
		services.AddSingleton<ICameraHandler, CameraHandler>();
		services.AddSingleton<IFileHandler, MobileFileHandler>();
		services.AddSingleton<ImageService>();
		services.AddSingleton<JsonHandler>();
		services.AddSingleton<InventoryJsonHandler>();
		services.AddSingleton(FileSystem.Current);
		services.AddSingleton(MediaPicker.Default);

		// Navigation abstraction
		services.AddSingleton<INavigationService, ShellNavigationService>();
		// Popup abstraction
		services.AddSingleton<IPopupService, MauiPopupService>();
		// Retry abstraction
		services.AddSingleton<IRetryService, RetryService>();
	}

    private static void RegisterDatabase(IServiceCollection services)
	{
        // Toggle the persistence backend.
        // - false: SQLite (current default)
        // - true: JSON operational store (multi-file + rollback)
        const bool UseJsonOperationalStore = false;

        if (UseJsonOperationalStore)
        {
            services.AddSingleton<JsonInventoryStore>();
            services.AddSingleton<IAppStartupInitializer, JsonStoreStartupInitializer>();
            services.AddSingleton<IInventoryMaintenanceService, JsonInventoryMaintenanceService>();

            // Focused domain repositories backed by JSON store
            services.AddSingleton<IContainerRepository, JsonContainerRepository>();
            services.AddSingleton<IItemRepository, JsonItemRepository>();
            services.AddSingleton<IImageRepository, JsonImageRepository>();
            services.AddSingleton<IRelationRepository, JsonRelationRepository>();

            services.AddSingleton<IInventoryDomainRepository, InventoryDomainRepository>();
            services.AddSingleton<IImagePathResolver, ImagePathResolver>();
        }
        else
        {
		    // Database and repositories
		    services.AddSingleton<MothballDatabase>();
		    services.AddSingleton<IAppStartupInitializer, SqliteStartupInitializer>();
		    services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));
		    // Focused domain repositories
		    services.AddSingleton<IContainerRepository, ContainerRepository>();
		    services.AddSingleton<IItemRepository, ItemRepository>();
		    services.AddSingleton<IImageRepository, ImageRepository>();
		    services.AddSingleton<IRelationRepository, RelationRepository>();
		    // Domain facade composing focused repositories
		    services.AddSingleton<IInventoryDomainRepository, InventoryDomainRepository>();
		    services.AddSingleton<IImagePathResolver, ImagePathResolver>();
        }
#if DEBUG
		services.AddSingleton<DemoDataSeeder>();
#endif
	}

    private static void RegisterViewModels(IServiceCollection services)
    {
        // ViewModels
        services.AddTransient<AddContainerViewModel>();
        services.AddTransient<ContainerListViewModel>();
        services.AddTransient<ItemsListViewModel>();
        services.AddTransient<ContainerDetailsViewModel>();
        services.AddTransient<ItemDetailsViewModel>();
        services.AddTransient<AddItemViewModel>();
		services.AddTransient<AddExistingItemToContainerViewModel>();
		services.AddTransient<AssociateItemWithContainerViewModel>();
    }
}
