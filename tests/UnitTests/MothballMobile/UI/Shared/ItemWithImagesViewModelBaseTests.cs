using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Moq;
using MothballMobile.Infrastructure;
using MothballMobile.UI.Features.Items.ItemsList;

namespace Mothball.Tests.Unit.Mobile.UI.Shared;

[TestFixture]
public sealed class ItemWithImagesViewModelBaseTests
{
    [Test]
    public void UpdateQuantities_RefreshesTotalsAndRaisesPropertyChanged()
    {
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var allocation = new ItemContainerAllocation(Guid.NewGuid(), "Box", 3);
        var inventory = new InventorySnapshot(item, 5, 3, [allocation]);
        var viewModel = new ItemViewModel(
            inventory,
            Mock.Of<IImagePathResolver>(),
            Mock.Of<INavigationService>(),
            showQuantityManagement: true,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask);

        var raisedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName);

        viewModel.UpdateQuantities(total: 8, assigned: 5, unassigned: 3);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.TotalQuantity, Is.EqualTo(8));
            Assert.That(viewModel.AssignedQuantity, Is.EqualTo(5));
            Assert.That(viewModel.UnassignedQuantity, Is.EqualTo(3));
            Assert.That(raisedProperties, Does.Contain(nameof(viewModel.TotalQuantity)));
            Assert.That(raisedProperties, Does.Contain(nameof(viewModel.AssignedQuantity)));
            Assert.That(raisedProperties, Does.Contain(nameof(viewModel.UnassignedQuantity)));
        });
    }
}
