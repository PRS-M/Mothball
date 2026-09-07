using Microsoft.Extensions.Logging;
using MothballMobile.Infrastructure.Scanning;

namespace MothballMobile;

public partial class AppShell : Shell
{
    private readonly ILogger<AppShell> logger;
    private readonly BarcodeLookupCoordinator barcodeLookupCoordinator;
    private readonly IPopupService popup;

    public AppShell(
        IPopupService popup,
        ILogger<AppShell> logger,
        BarcodeLookupCoordinator barcodeLookupCoordinator)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.barcodeLookupCoordinator = barcodeLookupCoordinator ?? throw new ArgumentNullException(nameof(barcodeLookupCoordinator));
        this.popup = popup ?? throw new ArgumentNullException(nameof(popup));
        InitializeComponent();
        ConfigureTopLevelNavigation();
        RegisterRoutes();
    }

    private void ConfigureTopLevelNavigation()
    {
        var localization = LocalizationManager.Current;
#if IOS || MACCATALYST
        var tabBar = new TabBar
        {
            Route = "MainTabs",
        };

        tabBar.Items.Add(CreateTab(localization.Get("Home"), "Home", "\uF015", typeof(MainPage)));
        tabBar.Items.Add(CreateTab(localization.Get("Containers"), "Containers", "\uF080", typeof(UI.Features.Containers.ContainersList.ContainersListPage)));
        tabBar.Items.Add(CreateTab(localization.Get("Items"), "Items", "\uF02C", typeof(UI.Features.Items.ItemsList.ItemsListPage)));
        tabBar.Items.Add(CreateTab(localization.Get("Tags"), "Tags", "\uF02B", typeof(UI.Features.Tags.TagsList.TagsListPage)));
        tabBar.Items.Add(CreateTab(localization.Get("Settings"), "Settings", "\uF013", typeof(UI.Features.Settings.SettingsPage)));

        Items.Add(tabBar);
#else
        Items.Add(CreateFlyoutItem(localization.Get("Home"), "Home", "\uF015", typeof(MainPage), "MainPage"));
        Items.Add(CreateFlyoutItem(localization.Get("Containers"), "Containers", "\uF080", typeof(UI.Features.Containers.ContainersList.ContainersListPage), "ContainersList"));
        Items.Add(CreateFlyoutItem(localization.Get("Items"), "Items", "\uF02C", typeof(UI.Features.Items.ItemsList.ItemsListPage), "ItemsList"));
        Items.Add(CreateFlyoutItem(localization.Get("Tags"), "Tags", "\uF02B", typeof(UI.Features.Tags.TagsList.TagsListPage), "TagsList"));
        Items.Add(CreateFlyoutItem(localization.Get("Settings"), "Settings", "\uF013", typeof(UI.Features.Settings.SettingsPage), "SettingsPage"));
#endif
    }

#if IOS || MACCATALYST
    private static Tab CreateTab(string title, string route, string glyph, Type pageType)
    {
        var tab = new Tab
        {
            Title = title,
            Route = route,
            Icon = CreateIcon(glyph),
        };
        tab.Items.Add(new ShellContent
        {
            Route = route + "Page",
            ContentTemplate = new DataTemplate(pageType),
        });
        return tab;
    }
#else
    private static FlyoutItem CreateFlyoutItem(
        string title,
        string route,
        string glyph,
        Type pageType,
        string pageRoute)
    {
        var flyoutItem = new FlyoutItem
        {
            Title = title,
            Route = route,
            Icon = CreateIcon(glyph),
        };
        var tab = new Tab { Title = title };
        tab.Items.Add(new ShellContent
        {
            Route = pageRoute,
            ContentTemplate = new DataTemplate(pageType),
        });
        flyoutItem.Items.Add(tab);
        return flyoutItem;
    }
#endif

    private static FontImageSource CreateIcon(string glyph)
        => new()
        {
            Glyph = glyph,
            FontFamily = "FontAwesomeSolid",
            Size = 26,
        };

    private async void OnBarcodeScanClicked(object? sender, EventArgs e)
    {
        try
        {
            await barcodeLookupCoordinator.ScanAndNavigateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Global barcode scan failed.");
            await popup.ShowAlertAsync(
                LocalizationManager.Current.Get("Error"),
                LocalizationManager.Current.Get("Something went wrong. Please try again."));
        }
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.HomeContainers, typeof(UI.Features.Containers.ContainersList.ContainersListPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.HomeItems, typeof(UI.Features.Items.ItemsList.ItemsListPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.Settings, typeof(UI.Features.Settings.SettingsPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.AdvancedSettings, typeof(UI.Features.Settings.AdvancedSettingsPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.ImportDocumentation, typeof(UI.Features.Settings.ImportDocumentationPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddContainer, typeof(UI.Features.Containers.AddContainer.AddContainerPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.ContainerDetails, typeof(UI.Features.Containers.ContainerDetails.ContainerDetailsPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.ItemDetails, typeof(UI.Features.Items.ItemDetails.ItemDetailsPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.ItemLocations, typeof(UI.Features.Items.ItemLocations.ItemLocationsPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddItem, typeof(UI.Features.Items.AddItem.AddItemPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.AddExistingItemToContainer, typeof(UI.Features.Containers.AddExistingItemToContainer.AddExistingItemToContainerPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.AssociateItemWithContainer, typeof(UI.Features.Containers.AssociateItemWithContainer.AssociateItemWithContainerPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.BackgroundOperations, typeof(UI.Features.BackgroundOperations.BackgroundOperationsPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.BarcodeScanner, typeof(UI.Features.Scanning.BarcodeScannerPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.Tags, typeof(UI.Features.Tags.TagsList.TagsListPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.TagResults, typeof(UI.Features.Tags.TagResults.TagResultsPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.TagItemPicker, typeof(UI.Features.Tags.TagAssignment.TagItemPickerPage));
        Routing.RegisterRoute(Infrastructure.NavigationRoutes.TagContainerPicker, typeof(UI.Features.Tags.TagAssignment.TagContainerPickerPage));
    }
}
