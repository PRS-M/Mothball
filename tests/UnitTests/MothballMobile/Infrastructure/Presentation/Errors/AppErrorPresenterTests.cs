using MothballMobile.Infrastructure.Presentation.Errors;

namespace Mothball.Tests.Unit.Mobile.Infrastructure.Presentation.Errors;

[TestFixture]
public sealed class AppErrorPresenterTests
{
    [Test]
    public void Show_WhenMessageIsPresent_ShowsTheMessage()
    {
        var presenter = new AppErrorPresenter();

        presenter.Show("Unable to save item.");

        Assert.Multiple(() =>
        {
            Assert.That(presenter.Message, Is.EqualTo("Unable to save item."));
            Assert.That(presenter.IsVisible, Is.True);
        });
    }

    [Test]
    public void Dismiss_HidesTheCurrentMessage()
    {
        var presenter = new AppErrorPresenter();
        presenter.Show("Unable to save item.");

        presenter.Dismiss();

        Assert.Multiple(() =>
        {
            Assert.That(presenter.Message, Is.Null);
            Assert.That(presenter.IsVisible, Is.False);
        });
    }
}