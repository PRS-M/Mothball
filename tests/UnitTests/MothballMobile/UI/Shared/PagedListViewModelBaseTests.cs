namespace Mothball.Tests.Unit.Mobile.UI.Shared;

[TestFixture]
public sealed class PagedListViewModelBaseTests
{
    [Test]
    public async Task InitializeAsync_AfterSuccessfulInitialization_DoesNotReloadUntilRefresh()
    {
        var viewModel = new CountingPagedListViewModel();

        await viewModel.InitializeAsync();
        await viewModel.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.LoadCallCount, Is.EqualTo(1));
            Assert.That(viewModel.Items, Is.EqualTo(new[] { 1 }));
        });

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.LoadCallCount, Is.EqualTo(2));
            Assert.That(viewModel.Items, Is.EqualTo(new[] { 2 }));
        });
    }

    [Test]
    public async Task InitializeAsync_AfterDataRevisionChanges_ReloadsCachedList()
    {
        var viewModel = new CountingPagedListViewModel();
        await viewModel.InitializeAsync();

        viewModel.MarkDataChanged();
        await viewModel.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.LoadCallCount, Is.EqualTo(2));
            Assert.That(viewModel.Items, Is.EqualTo(new[] { 2 }));
        });
    }

    [Test]
    public async Task InitializeAsync_ReportsQueryAndPopulationTimings()
    {
        var diagnostics = new RecordingPagedListLoadDiagnostics();
        var viewModel = new CountingPagedListViewModel(diagnostics);

        await viewModel.InitializeAsync();

        var measurement = diagnostics.Measurements.Single();
        Assert.Multiple(() =>
        {
            Assert.That(measurement.ListName, Is.EqualTo(nameof(CountingPagedListViewModel)));
            Assert.That(measurement.Variant, Is.EqualTo("browse"));
            Assert.That(measurement.PageNumber, Is.Zero);
            Assert.That(measurement.PageSize, Is.EqualTo(10));
            Assert.That(measurement.ResultCount, Is.EqualTo(1));
            Assert.That(measurement.QueryElapsedMilliseconds, Is.GreaterThanOrEqualTo(0));
            Assert.That(measurement.PopulationElapsedMilliseconds, Is.GreaterThanOrEqualTo(0));
            Assert.That(measurement.TotalElapsedMilliseconds, Is.GreaterThanOrEqualTo(0));
        });
    }

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

        protected override async Task<List<int>> LoadAsync(int pageNumber, int pageSize)
        {
            LoadCallCount++;
            loadStarted.TrySetResult();
            await releaseLoad.Task;
            return [1];
        }

        protected override int MapToViewModel(int source) => source;
    }

    private sealed class CountingPagedListViewModel : PagedListViewModelBase<int, int>
    {
        private long revision;

        public CountingPagedListViewModel(IPagedListLoadDiagnostics? diagnostics = null)
            : base(loadDiagnostics: diagnostics)
        {
        }

        public int LoadCallCount { get; private set; }
        protected override long DataRevision => revision;

        public void MarkDataChanged() => revision++;

        protected override Task<List<int>> LoadAsync(int pageNumber, int pageSize)
        {
            LoadCallCount++;
            return Task.FromResult<List<int>>([LoadCallCount]);
        }

        protected override int MapToViewModel(int source) => source;
    }

    private sealed class RecordingPagedListLoadDiagnostics : IPagedListLoadDiagnostics
    {
        public List<PagedListLoadMeasurement> Measurements { get; } = [];

        public void PageLoaded(PagedListLoadMeasurement measurement)
            => Measurements.Add(measurement);
    }
}
