using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Moq;
using MothballMobile.UI.Features.Items.Consumption;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Items;

[TestFixture]
public sealed class ItemConsumptionCoordinatorTests
{
    [Test]
    public async Task GeneralContext_SelectsSourceThenConsumesSelectedQuantity()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var containerId = Guid.NewGuid();
        var before = Snapshot(item, 5, new ItemContainerAllocation(containerId, "Box", 3));
        var after = Snapshot(item, 3, new ItemContainerAllocation(containerId, "Box", 1));
        var queries = DetailsSequence(item.ItemId, before, after);
        var commands = new Mock<IItemInventoryCommandService>(MockBehavior.Strict);
        commands.Setup(c => c.ConsumeAsync(
                item.ItemId,
                ItemInventoryConsumptionSource.FromContainer(containerId),
                2))
            .ReturnsAsync(new ItemInventoryUpdateResult(false, 3, 1, 2));
        var popup = new Mock<IPopupService>(MockBehavior.Strict);
        popup.Setup(p => p.SelectOptionAsync(It.IsAny<OptionPickerPopupDefinition<ItemInventoryConsumptionSource>>()))
            .ReturnsAsync(ItemInventoryConsumptionSource.FromContainer(containerId));
        popup.Setup(p => p.PickNumberAsync(It.Is<NumberPickerPopupDefinition>(d => d.Max == 3)))
            .ReturnsAsync(2);

        var result = await Create(queries.Object, commands.Object, popup.Object).ExecuteAsync(item.ItemId);

        Assert.That(result!.Inventory!.TotalQuantity, Is.EqualTo(3));
        commands.VerifyAll();
    }

    [Test]
    public async Task ContainerContext_AlwaysConfirmsPreferredSource()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var containerId = Guid.NewGuid();
        var before = Snapshot(item, 2, new ItemContainerAllocation(containerId, "Box", 1));
        var after = Snapshot(item, 1);
        var queries = DetailsSequence(item.ItemId, before, after);
        var commands = new Mock<IItemInventoryCommandService>();
        commands.Setup(c => c.ConsumeAsync(item.ItemId, It.IsAny<ItemInventoryConsumptionSource>(), 1))
            .ReturnsAsync(new ItemInventoryUpdateResult(true, 1, 0, 1));
        var popup = new Mock<IPopupService>(MockBehavior.Strict);
        popup.Setup(p => p.ConfirmAsync(It.Is<ConfirmationPopupDefinition>(d => d.Title == "Use from this container?")))
            .ReturnsAsync(true);
        popup.Setup(p => p.PickNumberAsync(It.IsAny<NumberPickerPopupDefinition>())).ReturnsAsync(1);

        await Create(queries.Object, commands.Object, popup.Object).ExecuteAsync(item.ItemId, containerId);

        popup.Verify(p => p.ConfirmAsync(It.IsAny<ConfirmationPopupDefinition>()), Times.Once);
        popup.Verify(p => p.SelectOptionAsync(It.IsAny<OptionPickerPopupDefinition<ItemInventoryConsumptionSource>>()), Times.Never);
    }

    [Test]
    public async Task DecliningPreferredSource_UsesGeneralPickerEvenWithSingleAllocation()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var containerId = Guid.NewGuid();
        var before = Snapshot(item, 2, new ItemContainerAllocation(containerId, "Box", 1));
        var queries = DetailsSequence(item.ItemId, before);
        var commands = new Mock<IItemInventoryCommandService>(MockBehavior.Strict);
        var popup = new Mock<IPopupService>(MockBehavior.Strict);
        popup.Setup(p => p.ConfirmAsync(It.IsAny<ConfirmationPopupDefinition>())).ReturnsAsync(false);
        popup.Setup(p => p.SelectOptionAsync(It.IsAny<OptionPickerPopupDefinition<ItemInventoryConsumptionSource>>()))
            .ReturnsAsync((ItemInventoryConsumptionSource?)null);

        var result = await Create(queries.Object, commands.Object, popup.Object).ExecuteAsync(item.ItemId, containerId);

        Assert.That(result, Is.Null);
        popup.Verify(p => p.SelectOptionAsync(It.IsAny<OptionPickerPopupDefinition<ItemInventoryConsumptionSource>>()), Times.Once);
    }

    private static ItemConsumptionCoordinator Create(
        IItemDetailsQueryHandler queries,
        IItemInventoryCommandService commands,
        IPopupService popup)
        => new(queries, commands, popup, new PopupDefinitionService());

    private static Mock<IItemDetailsQueryHandler> DetailsSequence(
        Guid itemId,
        params InventorySnapshot[] inventories)
    {
        var queries = new Mock<IItemDetailsQueryHandler>();
        var sequence = queries.SetupSequence(q => q.GetDetailsAsync(itemId.ToString()));
        foreach (var inventory in inventories)
        {
            sequence.ReturnsAsync(new ItemDetailsResult(inventory));
        }

        return queries;
    }

    private static InventorySnapshot Snapshot(
        Item item,
        int total,
        params ItemContainerAllocation[] allocations)
        => new(item, total, allocations.Sum(a => a.Quantity), allocations);
}
