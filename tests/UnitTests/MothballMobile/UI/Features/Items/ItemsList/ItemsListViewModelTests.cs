using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Application.Specifications;
using Moq;
using MothballMobile.UI.Features.Items.ItemsList;
using MothballMobile.UI.Features.Items.Consumption;
using MothballMobile.UI.Features.Items.ItemDetails;
using MothballMobile.UI.Features.Items.Quantity;
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.Infrastructure.BarcodeDocuments;
using CoreApp.Domain.Entities.Shared;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Items.ItemsList;

[TestFixture]
public sealed class ItemsListViewModelTests
{
    [Test]
    public async Task InitializeAsync_LoadsFirstPage()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var snapshots = new List<InventorySnapshot> { new(item, 1, 0, []) };
        var queries = new Mock<IItemsListQueryHandler>();
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10)).ReturnsAsync(snapshots);
        var viewModel = CreateViewModel(queries.Object);

        await viewModel.InitializeAsync();

        Assert.That(viewModel.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task InitializeAsync_PublishesRowsWithImagePathsAlreadyPopulated()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        item.AddImageItem();
        var queries = new Mock<IItemsListQueryHandler>();
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10))
            .ReturnsAsync([new InventorySnapshot(item, 1, 0, [])]);
        var paths = new Mock<IImagePathResolver>();
        paths.Setup(p => p.GetItemPhotoPaths(item)).Returns(["widget.jpg"]);
        var viewModel = CreateViewModel(queries.Object, paths: paths.Object);
        var publishedWithImage = false;
        viewModel.Items.CollectionChanged += (_, args) =>
            publishedWithImage = args.NewItems?[0] is ItemViewModel row
                && row.ImagePaths.SequenceEqual(["widget.jpg"]);

        await viewModel.InitializeAsync();

        Assert.That(publishedWithImage, Is.True);
    }

    [Test]
    public async Task SearchCommand_WithQuery_ReplacesListWithFilteredResults()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var queries = new Mock<IItemsListQueryHandler>();
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10)).ReturnsAsync([]);
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, "Widget", 0, 10))
            .ReturnsAsync([new InventorySnapshot(item, 1, 0, [])]);
        var diagnostics = new Mock<IPagedListLoadDiagnostics>();
        var viewModel = CreateViewModel(queries.Object, diagnostics: diagnostics.Object);
        await viewModel.InitializeAsync();

        viewModel.Query = "Widget";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.That(viewModel.Items, Has.Count.EqualTo(1));
        diagnostics.Verify(observer => observer.PageLoaded(
            It.Is<PagedListLoadMeasurement>(measurement =>
                measurement.Variant == "All:search"
                && !measurement.Variant.Contains("Widget", StringComparison.Ordinal))), Times.Once);
    }

    [Test]
    public async Task LoadNextPageCommand_DuringSearch_AppendsNextFilteredPage()
    {
        var firstPage = Enumerable.Range(1, 10)
            .Select(index => new InventorySnapshot(
                new Item(Guid.NewGuid(), $"Widget {index}", string.Empty),
                1,
                0,
                []))
            .ToList();
        var lastItem = new InventorySnapshot(
            new Item(Guid.NewGuid(), "Widget 11", string.Empty),
            1,
            0,
            []);
        var queries = new Mock<IItemsListQueryHandler>();
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10)).ReturnsAsync([]);
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, "Widget", 0, 10)).ReturnsAsync(firstPage);
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, "Widget", 1, 10)).ReturnsAsync([lastItem]);
        var viewModel = CreateViewModel(queries.Object);
        await viewModel.InitializeAsync();

        viewModel.Query = "Widget";
        await viewModel.SearchCommand.ExecuteAsync(null);
        await viewModel.LoadNextPageCommand.ExecuteAsync(null);

        Assert.That(viewModel.Items.Select(item => item.Name),
            Is.EqualTo(firstPage.Select(item => item.Item.Name).Append(lastItem.Item.Name)));
    }

    [Test]
    public async Task RefreshCommand_ReloadsFirstPage()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var queries = new Mock<IItemsListQueryHandler>();
        queries.SetupSequence(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10))
            .ReturnsAsync([])
            .ReturnsAsync([new InventorySnapshot(item, 1, 0, [])]);
        var viewModel = CreateViewModel(queries.Object);
        await viewModel.InitializeAsync();

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.That(viewModel.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DeleteCommand_WhenConfirmed_DeletesWholeItemAndRemovesRow()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var queries = new Mock<IItemsListQueryHandler>();
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10))
            .ReturnsAsync([new InventorySnapshot(item, 1, 0, [])]);
        var popup = new Mock<IPopupService>();
        popup.Setup(p => p.ConfirmAsync(It.IsAny<ConfirmationPopupDefinition>())).ReturnsAsync(true);
        var deleteHandler = new Mock<IDeleteItemCommandHandler>();
        var viewModel = CreateViewModel(queries.Object, popup.Object, deleteHandler.Object);
        await viewModel.InitializeAsync();

        await viewModel.Items.Single().DeleteCommand.ExecuteAsync(null);

        Assert.That(viewModel.Items, Is.Empty);
        deleteHandler.Verify(h => h.DeleteAsync(item.ItemId.ToString()), Times.Once);
    }

    [Test]
    public async Task SelectAllLoadedCommand_SelectsLoadedRowsAndShareCommandSharesOnlyBarcodedRows()
    {
        var barcodedItem = new Item(Guid.NewGuid(), "Widget", "");
        barcodedItem.UpdateBarcode(new Barcode("WIDGET-01", BarcodeSymbology.Code128));
        var unbarcodedItem = new Item(Guid.NewGuid(), "Cable", "");
        var queries = new Mock<IItemsListQueryHandler>();
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10))
            .ReturnsAsync([
                new InventorySnapshot(barcodedItem, 1, 0, []),
                new InventorySnapshot(unbarcodedItem, 1, 0, []),
            ]);
        var share = new Mock<IBarcodeShareService>();
        var viewModel = CreateViewModel(queries.Object, barcodeShare: share.Object);

        await viewModel.InitializeAsync();
        viewModel.EnterSelectionModeCommand.Execute(null);
        viewModel.SelectAllLoadedCommand.Execute(null);
        await viewModel.ShareSelectedCommand.ExecuteAsync(null);

        Assert.That(viewModel.SelectedCount, Is.EqualTo(2));
        share.Verify(service => service.ShareAsync(
            It.Is<IReadOnlyCollection<BarcodeLabelData>>(labels =>
                labels.Count == 1 && labels.Single().BarcodeValue == "WIDGET-01"),
            "Share item barcodes"), Times.Once);
    }

    [Test]
    public async Task ShareAllMatchingCommand_QueriesWithoutPagingAndPreservesSearch()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        item.UpdateBarcode(new Barcode("WIDGET-01", BarcodeSymbology.Code128));
        var queries = new Mock<IItemsListQueryHandler>();
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10)).ReturnsAsync([]);
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, "widget", null, null))
            .ReturnsAsync([new InventorySnapshot(item, 1, 0, [])]);
        var share = new Mock<IBarcodeShareService>();
        var viewModel = CreateViewModel(queries.Object, barcodeShare: share.Object);
        await viewModel.InitializeAsync();
        viewModel.Query = "widget";

        await viewModel.ShareAllMatchingCommand.ExecuteAsync(null);

        queries.Verify(q => q.QueryAsync(ItemQueryFilter.All, "widget", null, null), Times.Once);
    }

    [Test]
    public async Task ShareSelectedCommand_WhenNoSelectedRowsHaveBarcodes_RecordsLocalizedError()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var queries = new Mock<IItemsListQueryHandler>();
        queries.Setup(q => q.QueryAsync(ItemQueryFilter.All, null, 0, 10))
            .ReturnsAsync([new InventorySnapshot(item, 1, 0, [])]);
        var viewModel = CreateViewModel(queries.Object, barcodeShare: Mock.Of<IBarcodeShareService>());

        await viewModel.InitializeAsync();
        viewModel.EnterSelectionModeCommand.Execute(null);
        viewModel.SelectAllLoadedCommand.Execute(null);
        await viewModel.ShareSelectedCommand.ExecuteAsync(null);

        Assert.That(viewModel.ErrorMessage, Is.EqualTo("No barcode labels found."));
    }

    private static ItemsListViewModel CreateViewModel(
        IItemsListQueryHandler queries,
        IPopupService? popup = null,
        IDeleteItemCommandHandler? deleteHandler = null,
        IPagedListLoadDiagnostics? diagnostics = null,
        IImagePathResolver? paths = null,
        IBarcodeShareService? barcodeShare = null)
    {
        if (paths is null)
        {
            var pathMock = new Mock<IImagePathResolver>();
            pathMock.Setup(resolver => resolver.GetItemPhotoPaths(It.IsAny<Item>()))
                .Returns(Array.Empty<string>());
            paths = pathMock.Object;
        }
        popup ??= Mock.Of<IPopupService>();
        var details = Mock.Of<IItemDetailsQueryHandler>();
        var inventoryCommands = Mock.Of<IItemInventoryCommandService>();
        var definitions = new PopupDefinitionService();
        var withdrawal = new ItemInventoryWithdrawalCoordinator(inventoryCommands, popup, definitions);

        return new ItemsListViewModel(
            paths,
            queries,
            Mock.Of<INavigationService>(),
            Mock.Of<IApplicationSettings>(),
            new ItemQuantityEditCoordinator(details, inventoryCommands, withdrawal, popup, definitions),
            new ItemConsumptionCoordinator(details, inventoryCommands, popup, definitions),
            deleteHandler ?? Mock.Of<IDeleteItemCommandHandler>(),
            popup,
            definitions,
            Mock.Of<IInventoryChangeTracker>(),
            new BarcodeLookupCoordinator(
                Mock.Of<IBarcodeScanSession>(),
                Mock.Of<IInventoryQueryRepository>(),
                Mock.Of<INavigationService>()),
            Mock.Of<IBackgroundTaskObserver>(),
            loadDiagnostics: diagnostics,
            barcodeShare: barcodeShare);
    }
}
