using Microsoft.Maui.Controls;

namespace MothballMobile.Infrastructure.Navigation;

/// <summary>
/// MAUI Shell-based implementation of navigation services.
/// </summary>
public class ShellNavigationService : INavigationService
{
    /// <inheritdoc />
    public Task GoToAsync(string route)
        => Shell.Current.GoToAsync(route);

    /// <inheritdoc />
    public Task GoToAsync(string route, IDictionary<string, object> parameters)
        => Shell.Current.GoToAsync(route, parameters);

    /// <inheritdoc />
    public Task GoToAsync(string route, INavigationRequest request)
        => Shell.Current.GoToAsync(route, request.ToParameters());

    /// <inheritdoc />
    public Task GoBackAsync()
        => Shell.Current.GoToAsync("..");
}
