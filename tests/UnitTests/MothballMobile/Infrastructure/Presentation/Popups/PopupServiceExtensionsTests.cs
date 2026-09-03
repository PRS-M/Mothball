using Moq;

namespace Mothball.Tests.Unit.Mobile.Infrastructure.Presentation.Popups;

[TestFixture]
public sealed class PopupServiceExtensionsTests
{
    [Test]
    public async Task ConfirmAndRunAsync_WhenConfirmed_RunsActionAndReturnsTrue()
    {
        var popup = new Mock<IPopupService>();
        popup.Setup(p => p.ConfirmAsync(It.IsAny<ConfirmationPopupDefinition>())).ReturnsAsync(true);
        var ran = false;

        var result = await popup.Object.ConfirmAndRunAsync(
            new ConfirmationPopupDefinition("Delete", "Are you sure?", "Delete"),
            () =>
            {
                ran = true;
                return Task.CompletedTask;
            });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(ran, Is.True);
        });
    }

    [Test]
    public async Task ConfirmAndRunAsync_WhenDeclined_SkipsActionAndReturnsFalse()
    {
        var popup = new Mock<IPopupService>();
        popup.Setup(p => p.ConfirmAsync(It.IsAny<ConfirmationPopupDefinition>())).ReturnsAsync(false);
        var ran = false;

        var result = await popup.Object.ConfirmAndRunAsync(
            new ConfirmationPopupDefinition("Delete", "Are you sure?", "Delete"),
            () =>
            {
                ran = true;
                return Task.CompletedTask;
            });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(ran, Is.False);
        });
    }
}
