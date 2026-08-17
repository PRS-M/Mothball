using Infrastructure.Services;
using Infrastructure.Services.Database;
using Infrastructure.Services.Images;
using Infrastructure.Services.Restore;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;
using Infrastructure.Services.Repositories;
using Infrastructure.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using MothballMobile.UI.Features.Containers.AddContainer;
using MothballMobile.UI.Features.Containers.AddExistingItemToContainer;
using MothballMobile.UI.Features.Containers.AssociateItemWithContainer;
using MothballMobile.UI.Features.Containers.ContainerDetails;
using MothballMobile.UI.Features.Containers.ContainersList;
using MothballMobile.UI.Features.Items.AddItem;
using MothballMobile.UI.Features.Items.ItemDetails;
using MothballMobile.UI.Features.Items.ItemLocations;
using MothballMobile.UI.Features.Items.ItemsList;
using MothballMobile.UI.Features.Settings;

namespace MothballMobile.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreApplication(this IServiceCollection services)
    {
        services.AddTransient<IDebouncer>(sp => new Debouncer(300, sp.GetRequiredService<ILogger<Debouncer>>()));
        services.AddSingleton<JsonHandler>();
        services.AddSingleton<InventoryJsonHandler>();
        services.AddSingleton<IPhotoSourceReader, PhotoSourceReader>();
        services.AddSingleton<IPhotoFilePersistenceService, PhotoFilePersistenceService>();
        services.AddSingleton<ITemporaryPhotoService, TemporaryPhotoService>();
        services.AddSingleton<IPhotoDeletionService, PhotoDeletionService>();
        services.AddSingleton<ImageService>();

        services.AddSingleton<INavigationService, ShellNavigationService>();
        services.AddSingleton<IPopupService, MauiPopupService>();
        services.AddSingleton<IPopupDefinitionService, PopupDefinitionService>();
        services.AddSingleton<IRetryService, RetryService>();
        services.AddSingleton<IBackgroundTaskObserver, LoggingBackgroundTaskObserver>();
        services.AddSingleton<IPhotoBackgroundOperationTracker, PhotoBackgroundOperationTracker>();
        services.AddSingleton<IAppStartupOrchestrator, AppStartupOrchestrator>();
        services.AddSingleton<IApplicationSettings, ApplicationSettings>();
        services.AddSingleton<IInventoryBackupExporter, InventoryBackupExporter>();
        services.AddSingleton<InventoryBackupZipRestoreService>();
        services.AddSingleton<IInventoryBackupClient, NoopInventoryBackupClient>();
        services.AddSingleton<IItemInventoryCommandService, ItemInventoryCommandService>();
        services.AddSingleton<ContainerItemQuantityService>();
        services.AddSingleton<ContainerDetailsQueryHandler>();
        services.AddSingleton<IContainerAssociationQueryHandler, ContainerAssociationQueryHandler>();
        services.AddSingleton<IAssignItemToContainerCommandHandler, AssignItemToContainerCommandHandler>();
        services.AddSingleton<DeleteContainerCommandHandler>();
        services.AddSingleton<ContainerListQueryHandler>();
        services.AddSingleton<CreateContainerCommandHandler>();
        services.AddSingleton<ItemsListQueryHandler>();
        services.AddSingleton<IItemDetailsQueryHandler, ItemDetailsQueryHandler>();
        services.AddSingleton<ICreateItemCommandHandler, CreateItemCommandHandler>();
        services.AddSingleton<DeleteItemCommandHandler>();
        services.AddSingleton<UpdateItemDescriptionCommandHandler>();
        services.AddSingleton<UpdateContainerNotesCommandHandler>();

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
            services.AddSingleton<IItemInventoryRepository, JsonItemInventoryRepository>();
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
            services.AddSingleton<IItemInventoryRepository, ItemInventoryRepository>();
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
        services.AddSingleton<IShare>(Share.Default);
        services.AddSingleton<IFilePicker>(FilePicker.Default);
        services.AddSingleton(Preferences.Default);

        return services;
    }

    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<AddContainerViewModel>();
        services.AddTransient<ContainerListViewModel>();
        services.AddTransient<ItemsListViewModel>();
        services.AddTransient<ContainerDetailsViewModel>();
        services.AddTransient<ItemDetailsViewModel>();
        services.AddTransient<ItemLocationsViewModel>();
        services.AddTransient<AddItemViewModel>();
        services.AddTransient<AddExistingItemToContainerViewModel>();
        services.AddTransient<AssociateItemWithContainerViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services;
    }
}
