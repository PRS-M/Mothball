using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Navigation;

namespace Mothball.Tests.Unit.Mobile.Infrastructure.Navigation;

[TestFixture]
public sealed class NavigationRequestTests
{
    [Test]
    public void ItemDetailsRequest_WithSourceContainer_SerializesBothIdentifiers()
    {
        var itemId = Guid.NewGuid();
        var containerId = Guid.NewGuid();

        var parameters = new ItemDetailsNavigationRequest(itemId, containerId).ToParameters();

        Assert.Multiple(() =>
        {
            Assert.That(parameters[NavigationParams.ItemId], Is.EqualTo(itemId.ToString()));
            Assert.That(parameters[NavigationParams.ContainerId], Is.EqualTo(containerId.ToString()));
        });
    }

    [Test]
    public void ItemDetailsRequest_WithoutSourceContainer_OmitsContainerIdentifier()
    {
        var parameters = new ItemDetailsNavigationRequest(Guid.NewGuid()).ToParameters();

        Assert.That(parameters, Does.Not.ContainKey(NavigationParams.ContainerId));
    }

    [Test]
    public void AddItemRequest_WithoutContainer_SerializesNoParameters()
    {
        var parameters = new AddItemNavigationRequest().ToParameters();

        Assert.That(parameters, Is.Empty);
    }

    [Test]
    public void AddItemRequest_WithContainer_SerializesContainerIdentifier()
    {
        var containerId = Guid.NewGuid();

        var parameters = new AddItemNavigationRequest(containerId).ToParameters();

        Assert.That(parameters[NavigationParams.ContainerId], Is.EqualTo(containerId.ToString()));
    }

    [Test]
    public void AssociateItemRequest_SerializesQuantityAsInteger()
    {
        var itemId = Guid.NewGuid();

        var parameters = new AssociateItemWithContainerNavigationRequest(itemId, 4).ToParameters();

        Assert.Multiple(() =>
        {
            Assert.That(parameters[NavigationParams.ItemId], Is.EqualTo(itemId.ToString()));
            Assert.That(parameters[NavigationParams.UnassignedQuantity], Is.EqualTo(4));
        });
    }
}