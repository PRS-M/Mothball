using System.Collections.ObjectModel;
using CoreApp.Interfaces;
using CoreApp.Services;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.Shared;

public abstract class PhotoDetailsViewModelBase : BaseViewModel
{
    protected readonly IImagePathResolver paths;
    protected readonly ImageService imageService;
    protected readonly IRetryService retryService;
    private readonly IPhotoBackgroundOperationTracker photoBackgroundOperationTracker;

    private bool isImageResizeInProgress;
    private double imageResizeProgress;

    protected PhotoDetailsViewModelBase(
        IImagePathResolver paths,
        ImageService imageService,
        IRetryService retryService,
        IPhotoBackgroundOperationTracker photoBackgroundOperationTracker)
    {
        this.paths = paths;
        this.imageService = imageService;
        this.retryService = retryService;
        this.photoBackgroundOperationTracker = photoBackgroundOperationTracker;
    }

    public bool IsImageResizeInProgress
    {
        get => isImageResizeInProgress;
        private set => SetProperty(ref isImageResizeInProgress, value);
    }

    public double ImageResizeProgress
    {
        get => imageResizeProgress;
        private set => SetProperty(ref imageResizeProgress, value);
    }

    protected Task<bool> CaptureWithDefaultRetryAsync(Func<Task<bool>> attempt)
    {
        return retryService.RetryAsync(
            attempt: attempt,
            canceledTitle: "Photo capture canceled",
            canceledMessage: "Please try again or continue without a photo.",
            retryButton: "Retry",
            continueButton: "Continue",
            continueAlertTitle: "No photo",
            continueAlertMessage: "Continuing without a photo.");
    }

    protected Task<bool> CaptureWithDefaultRetryAndProgressAsync(Func<IProgress<double>, Task<bool>> attempt)
    {
        return CaptureWithDefaultRetryAsync(async () =>
        {
            IsImageResizeInProgress = true;
            ImageResizeProgress = 0;

            var progress = new Progress<double>(value =>
            {
                var normalized = Math.Clamp(value, 0, 1);
                MainThread.BeginInvokeOnMainThread(() => ImageResizeProgress = normalized);
            });

            try
            {
                return await attempt(progress);
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ImageResizeProgress = 1;
                    IsImageResizeInProgress = false;
                });
            }
        });
    }

    protected async Task CaptureTrackedPhotoAsync(
        string operationName,
        Func<IProgress<double>, Task<int>> captureAsync,
        ObservableCollection<string> targetPaths,
        Func<IEnumerable<string>> refreshedPaths,
        Func<bool>? shouldRefresh = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(captureAsync);
        ArgumentNullException.ThrowIfNull(targetPaths);
        ArgumentNullException.ThrowIfNull(refreshedPaths);

        Guid operationId = photoBackgroundOperationTracker.Start(operationName);
        var captured = false;

        try
        {
            captured = await CaptureWithDefaultRetryAndProgressAsync(
                attempt: async progress =>
                {
                    var compositeProgress = new Progress<double>(value =>
                    {
                        progress.Report(value);
                        photoBackgroundOperationTracker.Report(operationId, value);
                    });

                    return await captureAsync(compositeProgress) > 0;
                });
        }
        finally
        {
            photoBackgroundOperationTracker.Complete(operationId, captured);
        }

        if (!captured)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (shouldRefresh?.Invoke() == false)
            {
                return;
            }

            ReplaceWith(targetPaths, refreshedPaths());
        });
    }

    protected static void ReplaceWith<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
