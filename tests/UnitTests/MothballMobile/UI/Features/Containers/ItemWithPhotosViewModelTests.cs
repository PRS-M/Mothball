using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Moq;
using MothballMobile.UI.Features.Containers.ContainerDetails;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Containers;

[TestFixture]
public sealed class ItemWithPhotosViewModelTests
{
    [Test]
    public async Task UseCommand_PassesOwnerContainerAsPreferredSource()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var containerId = Guid.NewGuid();
        var inventory = new InventorySnapshot(
            item,
            3,
            2,
            [new ItemContainerAllocation(containerId, "Box", 2)]);
        var entry = new ContainerItemInventoryEntry(inventory, 2);
        Guid consumedItemId = Guid.Empty;
        Guid preferredContainerId = Guid.Empty;
        var viewModel = new ItemWithPhotosViewModel(
            entry,
            containerId,
            Mock.Of<IImagePathResolver>(),
            Mock.Of<INavigationService>(),
            Mock.Of<IPopupService>(),
            new PopupDefinitionService(),
            containerId.ToString(),
            showQuantityManagement: true,
            (_, _) => Task.CompletedTask,
            (itemId, preferredId) =>
            {
                consumedItemId = itemId;
                preferredContainerId = preferredId;
                return Task.CompletedTask;
            },
            () => { });

        await viewModel.UseCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(consumedItemId, Is.EqualTo(item.ItemId));
            Assert.That(preferredContainerId, Is.EqualTo(containerId));
        });
    }
}
