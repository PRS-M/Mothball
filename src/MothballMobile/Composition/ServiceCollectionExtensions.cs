using CoreApp.Interfaces;
using CoreApp.Services;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Infrastructure.Services.Restore;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;
using Infrastructure.Services.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Media;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Popups;
using MothballMobile.UI.Features.Containers.AddContainer;
using MothballMobile.UI.Features.Containers.AddExistingItemToContainer;
using MothballMobile.UI.Features.Containers.AssociateItemWithContainer;
using MothballMobile.UI.Features.Containers.ContainerDetails;
using MothballMobile.UI.Features.Containers.ContainersList;
using MothballMobile.UI.Features.Items.AddItem;
using MothballMobile.UI.Features.Items.ItemDetails;
using MothballMobile.UI.Features.Items.ItemsList;
using MothballMobile.UI.Features.Settings;

namespace MothballMobile.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreApplication(this IServiceCollection services)
    {
        services.AddTransient<IDebouncer>(sp => new Debouncer(300, sp.GetRequiredService<ILogger<Debouncer>>()));
        services.AddSingleton<ImageService>();
        services.AddSingleton<JsonHandler>();
        services.AddSingleton<InventoryJsonHandler>();

        services.AddSingleton<INavigationService, ShellNavigationService>();
        services.AddSingleton<IPopupService, MauiPopupService>();
        services.AddSingleton<IPopupDefinitionService, PopupDefinitionService>();
        services.AddSingleton<IRetryService, RetryService>();
        services.AddSingleton<IBackgroundTaskObserver, LoggingBackgroundTaskObserver>();
        services.AddSingleton<IPhotoBackgroundOperationTracker, PhotoBackgroundOperationTracker>();
        services.AddSingleton<IAppStartupOrchestrator, AppStartupOrchestrator>();
        services.AddSingleton<IInventoryBackupExporter, InventoryBackupExporter>();
        services.AddSingleton<IInventoryBackupService, InventoryBackupService>();
        services.AddSingleton<IInventoryBackupClient, NoopInventoryBackupClient>();
        services.AddSingleton<IContainerItemQuantityService, ContainerItemQuantityService>();

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var backend = configuration[PersistenceConfiguration.BackendKey];

        if (PersistenceConfiguration.UseJsonBackend(backend))
        {
            services.AddSingleton<JsonInventoryStore>();
            services.AddSingleton<IAppStartupInitializer, JsonStoreStartupInitializer>();
            services.AddSingleton<IInventoryMaintenanceService, JsonInventoryMaintenanceService>();

            services.AddSingleton<IContainerRepository, JsonContainerRepository>();
            services.AddSingleton<IItemRepository, JsonItemRepository>();
            services.AddSingleton<IImageRepository, JsonImageRepository>();
            services.AddSingleton<IRelationRepository, JsonRelationRepository>();

            services.AddSingleton<IInventoryQueryRepository, InventoryQueryRepository>();
            services.AddSingleton<IInventoryCommandRepository, InventoryCommandRepository>();
            services.AddSingleton<IImagePathResolver, ImagePathResolver>();
            services.AddSingleton<IInventoryBackupRestoreService, JsonInventoryBackupRestoreService>();
        }
        else
        {
            services.AddSingleton<MothballDatabase>();
            services.AddSingleton<IAppStartupInitializer, SqliteStartupInitializer>();
            services.AddSingleton<ITransactionRunner, SqliteTransactionRunner>();
            services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));

            services.AddSingleton<IContainerRepository, ContainerRepository>();
            services.AddSingleton<IItemRepository, ItemRepository>();
            services.AddSingleton<IImageRepository, ImageRepository>();
            services.AddSingleton<IRelationRepository, RelationRepository>();

            services.AddSingleton<IInventoryQueryRepository, InventoryQueryRepository>();
            services.AddSingleton<IInventoryCommandRepository, InventoryCommandRepository>();
            services.AddSingleton<IImagePathResolver, ImagePathResolver>();
            services.AddSingleton<IInventoryBackupRestoreService, SqliteInventoryBackupRestoreService>();
        }

#if DEBUG
        services.AddSingleton<DemoDataSeeder>();
#endif

        return services;
    }

    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<ICameraHandler, CameraHandler>();
        services.AddSingleton<IFileHandler, MobileFileHandler>();
        services.AddSingleton<IImageMetadataReader, SkiaImageMetadataReader>();
        services.AddSingleton(FileSystem.Current);
        services.AddSingleton(MediaPicker.Default);

        return services;
    }

    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<AddContainerViewModel>();
        services.AddTransient<ContainerListViewModel>();
        services.AddTransient<ItemsListViewModel>();
        services.AddTransient<ContainerDetailsViewModel>();
        services.AddTransient<ItemDetailsViewModel>();
        services.AddTransient<AddItemViewModel>();
        services.AddTransient<AddExistingItemToContainerViewModel>();
        services.AddTransient<AssociateItemWithContainerViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services;
    }
}
