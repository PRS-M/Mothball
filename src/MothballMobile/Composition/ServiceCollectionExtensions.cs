using Infrastructure.Services;
using Infrastructure.Services.Database;
using Infrastructure.Services.Images;
using Infrastructure.Services.Restore;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;
using Infrastructure.Services.Repositories;
using Infrastructure.Services.Startup;
using CoreApp.Application.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using MothballMobile.Infrastructure.Presentation.Errors;
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.Infrastructure.BarcodeDocuments;
using MothballMobile.Infrastructure.Localization;
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
using MothballMobile.UI.Features.Scanning;
using CoreApp.Application.Features.Barcodes.Commands;

namespace MothballMobile.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreApplication(this IServiceCollection services)
    {
        services.AddTransient<IDebouncer>(sp => new Debouncer(300, sp.GetRequiredService<ILogger<Debouncer>>()));
        services.AddSingleton<JsonHandler>();
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
        services.AddSingleton<IPagedListLoadDiagnostics, PagedListLoadDiagnostics>();
        services.AddSingleton<IPhotoBackgroundOperationTracker, PhotoBackgroundOperationTracker>();
        services.AddSingleton<IInventoryChangeTracker, InventoryChangeTracker>();
        services.AddSingleton<IAppStartupOrchestrator, AppStartupOrchestrator>();
        services.AddSingleton<IApplicationSettings, ApplicationSettings>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IBackupSignatureSecretProvider, BackupSignatureSecretProvider>();
        services.AddSingleton<IInventoryBackupWorkflowService, InventoryBackupWorkflowService>();
        services.AddSingleton<IBackupSigningKeyTransferService, BackupSigningKeyTransferService>();
        services.AddSingleton<IInventoryBackupExporter, InventoryBackupExporter>();
        services.AddSingleton<IInventoryBackupService, InventoryBackupService>();
        services.AddSingleton<IInventoryBackupZipRestoreService, InventoryBackupZipRestoreService>();
        services.AddSingleton<IInventoryBackupClient, NoopInventoryBackupClient>();
        services.AddSingleton<IItemInventoryCommandService, ItemInventoryCommandService>();
        services.AddSingleton<IItemReceiptService, ItemReceiptService>();
        services.AddSingleton<IContainerItemQuantityService, ContainerItemQuantityService>();
        services.AddSingleton<IBarcodeAssignmentService, BarcodeAssignmentService>();
        services.AddSingleton<IContainerDetailsQueryHandler, ContainerDetailsQueryHandler>();
        services.AddSingleton<IContainerDetailsHandler, ContainerDetailsHandler>();
        services.AddSingleton<IContainerAssociationQueryHandler, ContainerAssociationQueryHandler>();
        services.AddSingleton<IAssignItemToContainerCommandHandler, AssignItemToContainerCommandHandler>();
        services.AddSingleton<IContainerItemAssociationHandler, ContainerItemAssociationHandler>();
        services.AddSingleton<IDeleteContainerCommandHandler, DeleteContainerCommandHandler>();
        services.AddSingleton<IContainerListQueryHandler, ContainerListQueryHandler>();
        services.AddSingleton<ICreateContainerCommandHandler, CreateContainerCommandHandler>();
        services.AddSingleton<IItemsListQueryHandler, ItemsListQueryHandler>();
        services.AddSingleton<IItemDetailsQueryHandler, ItemDetailsQueryHandler>();
        services.AddSingleton<ICreateItemCommandHandler, CreateItemCommandHandler>();
        services.AddSingleton<IDeleteItemCommandHandler, DeleteItemCommandHandler>();
        services.AddSingleton<IUpdateItemDescriptionCommandHandler, UpdateItemDescriptionCommandHandler>();
        services.AddSingleton<IUpdateContainerNotesCommandHandler, UpdateContainerNotesCommandHandler>();

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
        services.AddSingleton<IBarcodeScanSession, BarcodeScanSession>();
        services.AddSingleton<BarcodeLookupCoordinator>();
        services.AddSingleton<IBarcodeLabelDocumentGenerator, SkiaBarcodeLabelDocumentGenerator>();
        services.AddSingleton<IBarcodeShareService, BarcodeShareService>();
        services.AddSingleton<ICameraHandler, CameraHandler>();
        services.AddSingleton<IFileHandler, MobileFileHandler>();
        services.AddSingleton<IImageMetadataReader, SkiaImageMetadataReader>();
        services.AddSingleton(FileSystem.Current);
        services.AddSingleton(MediaPicker.Default);
        services.AddSingleton<IShare>(Share.Default);
        services.AddSingleton<IFilePicker>(FilePicker.Default);
        services.AddSingleton(Preferences.Default);
        services.AddSingleton<ISecureStorage>(SecureStorage.Default);

        return services;
    }

    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<ContainerDetailsItemsCoordinator>();
        services.AddTransient<AssociateItemWithContainerCoordinator>();
        services.AddTransient<ItemInventoryWithdrawalCoordinator>();
        services.AddTransient<UI.Features.Items.Consumption.ItemConsumptionCoordinator>();
        services.AddTransient<UI.Features.Items.Quantity.ItemQuantityEditCoordinator>();
        services.AddTransient<ItemDetailsCoordinator>();
        services.AddTransient<AddContainerViewModel>();
        services.AddTransient<ContainerListViewModel>();
        services.AddTransient<ItemsListViewModel>();
        services.AddTransient<ContainerDetailsViewModel>();
        services.AddTransient<ItemDetailsViewModel>();
        services.AddTransient<ItemLocationsViewModel>();
        services.AddTransient<AddItemViewModel>();
        services.AddTransient<AddExistingItemToContainerViewModel>();
        services.AddTransient<AssociateItemWithContainerViewModel>();
        services.AddTransient<AppearanceSettingsViewModel>();
        services.AddTransient<BackupSettingsViewModel>();
        services.AddTransient<BackupSigningKeySettingsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BarcodeScannerViewModel>();

        return services;
    }
}
