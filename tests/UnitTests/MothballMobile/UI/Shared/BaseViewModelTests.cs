namespace Mothball.Tests.Unit.Mobile.UI.Shared;

[TestFixture]
public sealed class BaseViewModelTests
{
    [Test]
    public async Task RunCommandAsync_WhenActionFails_RecordsErrorAndRethrows()
    {
        var viewModel = new TestViewModel();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await viewModel.RunAsync(() => throw new InvalidOperationException("Store unavailable.")));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo("Store unavailable."));
            Assert.That(viewModel.ErrorMessage, Is.EqualTo("Store unavailable."));
            Assert.That(viewModel.HasError, Is.True);
            Assert.That(viewModel.IsBusy, Is.False);
        });
    }

    [Test]
    public async Task RunCommandAsync_WhenNextActionSucceeds_ClearsPreviousError()
    {
        var viewModel = new TestViewModel();

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await viewModel.RunAsync(() => throw new InvalidOperationException("Store unavailable.")));

        await viewModel.RunAsync(() => Task.CompletedTask);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ErrorMessage, Is.Null);
            Assert.That(viewModel.HasError, Is.False);
        });
    }

    [Test]
    public async Task RunCommandAsync_WhenActionIsCanceled_DoesNotPublishAnError()
    {
        var viewModel = new TestViewModel();
        var errorRaised = false;
        viewModel.ErrorOccurred += _ => errorRaised = true;

        var exception = Assert.ThrowsAsync<OperationCanceledException>(
            async () => await viewModel.RunAsync(() => Task.FromException(new OperationCanceledException("Superseded."))));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(errorRaised, Is.False);
            Assert.That(viewModel.ErrorMessage, Is.Null);
            Assert.That(viewModel.HasError, Is.False);
            Assert.That(viewModel.IsBusy, Is.False);
        });
    }

    private sealed class TestViewModel : BaseViewModel
    {
        public Task RunAsync(Func<Task> action) => RunCommandAsync(action);
    }
}
