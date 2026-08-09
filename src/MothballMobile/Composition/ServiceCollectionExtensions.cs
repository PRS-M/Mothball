using CoreApp.Interfaces;
using CoreApp.Services;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;
using Infrastructure.Services.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Media;
using MothballMobile.Infrastructure;
using MothballMobile.UI.Features.Containers.AddContainer;
using MothballMobile.UI.Features.Containers.AddExistingItemToContainer;
using MothballMobile.UI.Features.Containers.AssociateItemWithContainer;
using MothballMobile.UI.Features.Containers.ContainerDetails;
using MothballMobile.UI.Features.Containers.ContainersList;
using MothballMobile.UI.Features.Items.AddItem;
using MothballMobile.UI.Features.Items.ItemDetails;
using MothballMobile.UI.Features.Items.ItemsList;

namespace MothballMobile.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreApplication(this IServiceCollection services)
    {
        services.AddTransient<IDebouncer>(_ => new Debouncer(300));
        services.AddSingleton<ImageService>();
        services.AddSingleton<JsonHandler>();
        services.AddSingleton<InventoryJsonHandler>();

        services.AddSingleton<INavigationService, ShellNavigationService>();
        services.AddSingleton<IPopupService, MauiPopupService>();
        services.AddSingleton<IRetryService, RetryService>();
        services.AddSingleton<IAppStartupOrchestrator, AppStartupOrchestrator>();

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

        return services;
    }
}
