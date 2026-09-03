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

    private sealed class TestViewModel : BaseViewModel
    {
        public Task RunAsync(Func<Task> action) => RunCommandAsync(action);
    }
}