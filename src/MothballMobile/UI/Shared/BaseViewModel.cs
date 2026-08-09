using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MothballMobile.UI.Shared;

public abstract class BaseViewModel : ObservableObject
{
    private bool isBusy;
    private bool isRefreshing;

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    public bool IsRefreshing
    {
        get => isRefreshing;
        set => SetProperty(ref isRefreshing, value);
    }

    protected async Task RunCommandAsync(Func<Task> action, bool showRefreshing = false)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        if (showRefreshing)
            IsRefreshing = true;

        try
        {
            await action();
        }
        finally
        {
            if (showRefreshing)
                IsRefreshing = false;

            IsBusy = false;
        }
    }
}
