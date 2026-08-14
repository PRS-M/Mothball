using System.Collections.ObjectModel;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Services;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Popups;

namespace MothballMobile.UI.Shared;

public abstract class PhotoDetailsViewModelBase : BaseViewModel
{
    protected readonly IImagePathResolver paths;
    protected readonly ImageService imageService;
    protected readonly IPopupService popup;
    protected readonly IPopupDefinitionService popupDefinitions;
    private readonly IPhotoBackgroundOperationTracker photoBackgroundOperationTracker;

    private bool isPhotoCaptureInProgress;

    protected PhotoDetailsViewModelBase(
        IImagePathResolver paths,
        ImageService imageService,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        IPhotoBackgroundOperationTracker photoBackgroundOperationTracker)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        this.popup = popup ?? throw new ArgumentNullException(nameof(popup));
        this.popupDefinitions = popupDefinitions ?? throw new ArgumentNullException(nameof(popupDefinitions));
        this.photoBackgroundOperationTracker = photoBackgroundOperationTracker ?? throw new ArgumentNullException(nameof(photoBackgroundOperationTracker));
    }

    protected bool IsPhotoCaptureInProgress
    {
        get => isPhotoCaptureInProgress;
        private set => SetProperty(ref isPhotoCaptureInProgress, value);
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

        Guid? operationId = null;
        var captured = false;

        IsPhotoCaptureInProgress = true;

        try
        {
            var progress = new Progress<double>(value =>
            {
                var normalized = Math.Clamp(value, 0, 1);

                operationId ??= photoBackgroundOperationTracker.Start(operationName);
                photoBackgroundOperationTracker.Report(operationId.Value, normalized);
            });

            captured = await captureAsync(progress) > 0;
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsPhotoCaptureInProgress = false;
            });

            if (operationId.HasValue)
            {
                if (captured)
                {
                    photoBackgroundOperationTracker.Report(operationId.Value, 1);
                }

                photoBackgroundOperationTracker.Complete(operationId.Value, captured);
            }
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

    protected async Task<ImageItem?> SelectPhotoAsync(OptionPickerPopupDefinition<ImageItem> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return await popup.SelectOptionAsync(definition);
    }

    protected async Task<PhotoSource?> SelectPhotoSourceAsync()
        => await PhotoSourceSelector.SelectPhotoSourceAsync(popup, popupDefinitions);
}
