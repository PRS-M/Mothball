using CoreApp.Domain.Entities.ContainerAggregate;
using Moq;
using MothballMobile.UI.Features.Containers.ContainersList;

namespace Mothball.Tests.Unit.Mobile.UI.Features.Containers;

[TestFixture]
public sealed class ContainerViewModelTests
{
    [Test]
    public void ItemsStoredText_InSimpleMode_UsesItemTypesLabelAndCount()
    {
        var container = new Container(Guid.NewGuid(), "Box", string.Empty);
        container.SetItemSummary(itemTypeCount: 3, totalItemQuantity: 25);
        var viewModel = new ContainerViewModel(container, Mock.Of<IImagePathResolver>(), Mock.Of<INavigationService>(), false);

        Assert.That(viewModel.ItemsStoredText, Is.EqualTo("Item types stored: 3"));
    }

    [Test]
    public void ItemsStoredText_InAdvancedMode_UsesTotalLabelAndQuantity()
    {
        var container = new Container(Guid.NewGuid(), "Box", string.Empty);
        container.SetItemSummary(itemTypeCount: 3, totalItemQuantity: 25);
        var viewModel = new ContainerViewModel(container, Mock.Of<IImagePathResolver>(), Mock.Of<INavigationService>(), true);

        Assert.That(viewModel.ItemsStoredText, Is.EqualTo("Items stored (Total): 25"));
    }
}
