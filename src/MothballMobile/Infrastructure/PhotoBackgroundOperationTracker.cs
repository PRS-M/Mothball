using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MothballMobile.Infrastructure;

public sealed class PhotoBackgroundOperationTracker : ObservableObject, IPhotoBackgroundOperationTracker
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, OperationState> activeOperations = new();
    private readonly ObservableCollection<PhotoBackgroundOperationEntry> recentOperations = new();

    private const int MaxRecentOperations = 25;

    private int activeOperationCount;
    private double overallProgress;
    private string statusText = string.Empty;
    private bool isBannerVisible;
    private CancellationTokenSource? hideBannerCts;

    public int ActiveOperationCount
    {
        get => activeOperationCount;
        private set => SetProperty(ref activeOperationCount, value);
    }

    public double OverallProgress
    {
        get => overallProgress;
        private set => SetProperty(ref overallProgress, value);
    }

    public string StatusText
    {
        get => statusText;
        private set => SetProperty(ref statusText, value);
    }

    public bool IsBannerVisible
    {
        get => isBannerVisible;
        private set => SetProperty(ref isBannerVisible, value);
    }

    public IReadOnlyList<PhotoBackgroundOperationEntry> RecentOperations => recentOperations;

    public Guid Start(string operationDescription)
    {
        var operationId = Guid.NewGuid();

        lock (gate)
        {
            hideBannerCts?.Cancel();
            activeOperations[operationId] = new OperationState(operationDescription, DateTimeOffset.UtcNow, 0);
            PublishRunningState();
        }

        return operationId;
    }

    public void Report(Guid operationId, double progress)
    {
        lock (gate)
        {
            if (!activeOperations.TryGetValue(operationId, out var state))
            {
                return;
            }

            state.Progress = Math.Clamp(progress, 0, 1);
            PublishRunningState();
        }
    }

    public void Complete(Guid operationId, bool success)
    {
        lock (gate)
        {
            if (!activeOperations.TryGetValue(operationId, out var completedOperation))
            {
                return;
            }

            activeOperations.Remove(operationId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                recentOperations.Insert(0, new PhotoBackgroundOperationEntry
                {
                    Description = completedOperation.Description,
                    Succeeded = success,
                    CompletedAt = DateTimeOffset.Now,
                    FinalProgress = completedOperation.Progress
                });

                while (recentOperations.Count > MaxRecentOperations)
                {
                    recentOperations.RemoveAt(recentOperations.Count - 1);
                }
            });

            if (activeOperations.Count > 0)
            {
                PublishRunningState();
                return;
            }

            PublishState(
                activeCount: 0,
                progress: 1,
                status: success ? "Photo saved in background." : "Photo operation ended.",
                bannerVisible: true);

            hideBannerCts?.Cancel();
            hideBannerCts = new CancellationTokenSource();
            var token = hideBannerCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), token);
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    PublishState(activeCount: 0, progress: 0, status: string.Empty, bannerVisible: false);
                }
                catch (OperationCanceledException)
                {
                    // Ignore cancellation when newer operations replace this hide timer.
                }
            }, token);
        }
    }

    private void PublishRunningState()
    {
        int count = activeOperations.Count;
        double progress = count == 0 ? 0 : activeOperations.Values.Average(x => x.Progress);

        string status;
        if (count == 1)
        {
            var first = activeOperations.Values.First();
            status = string.Concat(first.Description, " in background...");
        }
        else
        {
            status = $"{count} photo operations running in background...";
        }

        PublishState(count, progress, status, bannerVisible: true);
    }

    private void PublishState(int activeCount, double progress, string status, bool bannerVisible)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ActiveOperationCount = activeCount;
            OverallProgress = Math.Clamp(progress, 0, 1);
            StatusText = status;
            IsBannerVisible = bannerVisible;
        });
    }

    private sealed class OperationState
    {
        public OperationState(string description, DateTimeOffset startedAt, double progress)
        {
            Description = description;
            StartedAt = startedAt;
            Progress = progress;
        }

        public string Description { get; }
        public DateTimeOffset StartedAt { get; }
        public double Progress { get; set; }
    }
}
