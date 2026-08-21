using System.Collections.ObjectModel;
using CoreApp.Domain.Entities.Shared;

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

    /// <summary>
    /// Captures a photo, tracks its background progress, and refreshes the target paths on success.
    /// </summary>
    /// <param name="operationName">The name shown for the tracked operation.</param>
    /// <param name="captureAsync">The photo capture operation that reports progress.</param>
    /// <param name="targetPaths">The collection to update after a successful capture.</param>
    /// <param name="refreshedPaths">Provides the refreshed collection of photo paths.</param>
    /// <param name="shouldRefresh">Optionally determines whether the paths should be refreshed.</param>
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

    /// <summary>
    /// Replaces the contents of a collection with a new sequence.
    /// </summary>
    /// <typeparam name="T">The type contained in the collection.</typeparam>
    /// <param name="target">The collection to replace.</param>
    /// <param name="items">The items to add to the collection.</param>
    protected static void ReplaceWith<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    /// <summary>
    /// Displays a picker for selecting a photo.
    /// </summary>
    /// <param name="definition">The picker definition to display.</param>
    /// <returns>The selected photo, or <see langword="null"/> when cancelled.</returns>
    protected async Task<ImageItem?> SelectPhotoAsync(OptionPickerPopupDefinition<ImageItem> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return await popup.SelectOptionAsync(definition);
    }

    protected async Task<PhotoSource?> SelectPhotoSourceAsync()
        => await PhotoSourceSelector.SelectPhotoSourceAsync(popup, popupDefinitions);

    /// <summary>
    /// Prompts to pick a photo to delete, confirms, deletes it, and refreshes the target paths on success.
    /// </summary>
    /// <param name="hasPhotos">Whether the owning entity currently has any photos.</param>
    /// <param name="noPhotosPopup">Shown when there are no photos to delete.</param>
    /// <param name="pickerDefinition">Lists the photos to choose from.</param>
    /// <param name="deleteAsync">Deletes the selected photo and reports whether it was removed.</param>
    /// <param name="targetPaths">The collection to refresh after a successful delete.</param>
    /// <param name="refreshedPaths">Provides the refreshed collection of photo paths.</param>
    protected async Task DeleteSelectedPhotoAsync(
        bool hasPhotos,
        AlertPopupDefinition noPhotosPopup,
        OptionPickerPopupDefinition<ImageItem> pickerDefinition,
        Func<Guid, Task<bool>> deleteAsync,
        ObservableCollection<string> targetPaths,
        Func<IEnumerable<string>> refreshedPaths)
    {
        if (!hasPhotos)
        {
            await popup.ShowAlertAsync(noPhotosPopup);
            return;
        }

        var selectedPhoto = await SelectPhotoAsync(pickerDefinition);
        if (selectedPhoto is null)
        {
            return;
        }

        await popup.ConfirmAndRunAsync(popupDefinitions.DeletePhoto(), () => RunCommandAsync(async () =>
        {
            var deleted = await deleteAsync(selectedPhoto.ImageId);
            if (deleted)
            {
                ReplaceWith(targetPaths, refreshedPaths());
            }
        }));
    }
}
