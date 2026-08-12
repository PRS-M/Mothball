using CommunityToolkit.Mvvm.ComponentModel;

namespace MothballMobile.Infrastructure;

public sealed class PhotoBackgroundOperationTracker : ObservableObject, IPhotoBackgroundOperationTracker
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, double> activeProgressByOperation = new();

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

    public Guid Start(string operationDescription)
    {
        var operationId = Guid.NewGuid();

        lock (gate)
        {
            hideBannerCts?.Cancel();
            activeProgressByOperation[operationId] = 0;
            PublishRunningState(operationDescription);
        }

        return operationId;
    }

    public void Report(Guid operationId, double progress)
    {
        lock (gate)
        {
            if (!activeProgressByOperation.ContainsKey(operationId))
            {
                return;
            }

            activeProgressByOperation[operationId] = Math.Clamp(progress, 0, 1);
            PublishRunningState("Processing photos");
        }
    }

    public void Complete(Guid operationId, bool success)
    {
        lock (gate)
        {
            activeProgressByOperation.Remove(operationId);

            if (activeProgressByOperation.Count > 0)
            {
                PublishRunningState("Processing photos");
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

    private void PublishRunningState(string description)
    {
        int count = activeProgressByOperation.Count;
        double progress = count == 0 ? 0 : activeProgressByOperation.Values.Average();

        string status = count == 1
            ? string.Concat(description, " in background...")
            : $"{count} photo operations running in background...";

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
}
