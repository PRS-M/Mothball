namespace Mothball.Tests.Unit.Mobile.UI.Shared;

[TestFixture]
public sealed class PagedListViewModelBaseTests
{
    [Test]
    public async Task LoadNextPage_WhenAnotherPageIsLoading_DoesNotStartAnOverlappingRequest()
    {
        var viewModel = new BlockingPagedListViewModel();

        var firstLoad = viewModel.LoadNextPage();
        await viewModel.LoadStarted;
        await viewModel.LoadNextPage();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsBusy, Is.True);
            Assert.That(viewModel.LoadCallCount, Is.EqualTo(1));
        });

        viewModel.ReleaseLoad();
        await firstLoad;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsBusy, Is.False);
            Assert.That(viewModel.Items, Is.EqualTo(new[] { 1 }));
        });
    }

    private sealed class BlockingPagedListViewModel : PagedListViewModelBase<int, int>
    {
        private readonly TaskCompletionSource loadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task LoadStarted => loadStarted.Task;
        public int LoadCallCount { get; private set; }

        public void ReleaseLoad() => releaseLoad.TrySetResult();

        protected override Task EnsureDummyData() => Task.CompletedTask;

        protected override async Task<List<int>> LoadAsync(int pageNumber, int pageSize)
        {
            LoadCallCount++;
            loadStarted.TrySetResult();
            await releaseLoad.Task;
            return [1];
        }

        protected override int MapToViewModel(int source) => source;
    }
}
