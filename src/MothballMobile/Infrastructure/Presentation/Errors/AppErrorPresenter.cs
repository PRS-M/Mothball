using CommunityToolkit.Mvvm.ComponentModel;

namespace MothballMobile.Infrastructure.Presentation.Errors;

public sealed partial class AppErrorPresenter : ObservableObject, IAppErrorPresenter
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisible))]
    private string? message;

    public bool IsVisible => !string.IsNullOrWhiteSpace(Message);

    public void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Message = message;
    }

    public void Dismiss() => Message = null;
}