using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Moq;
using MothballMobile.UI.Features.Items.ItemDetails;
using MothballMobile.UI.Features.Items.Quantity;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Items.Quantity;

[TestFixture]
public sealed class ItemQuantityEditCoordinatorTests
{
    [Test]
    public async Task ExecuteAsync_UnassignedFirst_WhenUnassignedCoversDecrease_LeavesAllocationsUntouched()
    {
        var allocation = new ItemContainerAllocation(Guid.NewGuid(), "Box", 6);
        var inventory = CreateInventory(total: 10, allocation);
        var commands = new Mock<IItemInventoryCommandService>(MockBehavior.Strict);
        commands.Setup(service => service.ApplyWithdrawalAsync(
                inventory.Item.ItemId,
                It.Is<ItemInventoryWithdrawalPlan>(plan =>
                    plan.TotalQuantity == 8
                    && plan.AssignedQuantity == 6
                    && plan.UnassignedQuantity == 2
                    && plan.Allocations.SequenceEqual(inventory.Allocations))))
            .ReturnsAsync(new ItemInventoryUpdateResult(false, 8, 6, 2));
        var popup = CreateQuantityPopup(8);
        var coordinator = CreateCoordinator(inventory, commands.Object, popup.Object);

        var result = await coordinator.ExecuteAsync(
            inventory.Item.ItemId,
            decreasePreference: ItemQuantityDecreasePreference.UnassignedFirst);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Inventory!.TotalQuantity, Is.EqualTo(8));
            Assert.That(result.Inventory.AssignedQuantity, Is.EqualTo(6));
            Assert.That(result.Inventory.UnassignedQuantity, Is.EqualTo(2));
        });
        popup.Verify(service => service.SelectOptionAsync(
            It.IsAny<OptionPickerPopupDefinition<ItemContainerAllocation>>()), Times.Never);
        popup.Verify(service => service.PickNumberAsync(
            It.Is<NumberPickerPopupDefinition>(definition => definition.Title.StartsWith("Withdraw from"))), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_UnassignedFirst_WhenDecreaseExceedsUnassigned_PromptsForRemainderOnly()
    {
        var allocation = new ItemContainerAllocation(Guid.NewGuid(), "Box", 7);
        var inventory = CreateInventory(total: 10, allocation);
        var commands = new Mock<IItemInventoryCommandService>(MockBehavior.Strict);
        commands.Setup(service => service.ApplyWithdrawalAsync(
                inventory.Item.ItemId,
                It.Is<ItemInventoryWithdrawalPlan>(plan =>
                    plan.TotalQuantity == 5
                    && plan.AssignedQuantity == 5
                    && plan.UnassignedQuantity == 0
                    && plan.Allocations.Single().Quantity == 5)))
            .ReturnsAsync(new ItemInventoryUpdateResult(false, 5, 5, 0));
        var popup = CreateQuantityPopup(5);
        popup.Setup(service => service.SelectOptionAsync(
                It.IsAny<OptionPickerPopupDefinition<ItemContainerAllocation>>()))
            .ReturnsAsync(allocation);
        popup.Setup(service => service.PickNumberAsync(
                It.Is<NumberPickerPopupDefinition>(definition => definition.Title == "Withdraw from Box")))
            .ReturnsAsync(2);
        var coordinator = CreateCoordinator(inventory, commands.Object, popup.Object);

        var result = await coordinator.ExecuteAsync(
            inventory.Item.ItemId,
            decreasePreference: ItemQuantityDecreasePreference.UnassignedFirst);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Inventory!.TotalQuantity, Is.EqualTo(5));
            Assert.That(result.Inventory.AssignedQuantity, Is.EqualTo(5));
            Assert.That(result.Inventory.UnassignedQuantity, Is.Zero);
        });
        popup.Verify(service => service.PickNumberAsync(
            It.Is<NumberPickerPopupDefinition>(definition =>
                definition.Title == "Withdraw from Box" && definition.InitialValue == 2)), Times.Once);
    }

    private static InventorySnapshot CreateInventory(
        int total,
        ItemContainerAllocation allocation)
        => new(
            new Item(Guid.NewGuid(), "Widget", string.Empty),
            total,
            allocation.Quantity,
            [allocation]);

    private static Mock<IPopupService> CreateQuantityPopup(int selectedTotal)
    {
        var popup = new Mock<IPopupService>(MockBehavior.Strict);
        popup.Setup(service => service.PickNumberAsync(
                It.Is<NumberPickerPopupDefinition>(definition => definition.Title == "Set total quantity")))
            .ReturnsAsync(selectedTotal);
        return popup;
    }

    private static ItemQuantityEditCoordinator CreateCoordinator(
        InventorySnapshot inventory,
        IItemInventoryCommandService commands,
        IPopupService popup)
    {
        var details = new Mock<IItemDetailsQueryHandler>();
        details.Setup(handler => handler.GetDetailsAsync(inventory.Item.ItemId.ToString()))
            .ReturnsAsync(new ItemDetailsResult(inventory));
        var definitions = new PopupDefinitionService();
        return new ItemQuantityEditCoordinator(
            details.Object,
            commands,
            new ItemInventoryWithdrawalCoordinator(commands, popup, definitions),
            popup,
            definitions);
    }
}
