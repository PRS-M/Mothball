using System.Collections.ObjectModel;
using CoreApp.Interfaces;
using CoreApp.Services;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.ViewModels;

public abstract class PhotoDetailsViewModelBase : BaseViewModel
{
    protected readonly IImagePathResolver paths;
    protected readonly ImageService imageService;
    protected readonly IRetryService retryService;

    protected PhotoDetailsViewModelBase(IImagePathResolver paths, ImageService imageService, IRetryService retryService)
    {
        this.paths = paths;
        this.imageService = imageService;
        this.retryService = retryService;
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

    protected static void ReplaceWith<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
