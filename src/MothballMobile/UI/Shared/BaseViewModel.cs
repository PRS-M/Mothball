using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MothballMobile.UI.Shared;

/// <summary>
/// Provides common busy-state management for view models.
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    private bool isBusy;
    private bool isRefreshing;

    /// <summary>
    /// Gets or sets whether the view model is executing a command.
    /// </summary>
    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    /// <summary>
    /// Gets or sets whether the view model is refreshing its data.
    /// </summary>
    public bool IsRefreshing
    {
        get => isRefreshing;
        set => SetProperty(ref isRefreshing, value);
    }

    /// <summary>
    /// Runs an asynchronous command while managing busy and optional refresh state.
    /// </summary>
    /// <param name="action">The command to run.</param>
    /// <param name="showRefreshing">Whether to set <see cref="IsRefreshing"/> while the command runs.</param>
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
