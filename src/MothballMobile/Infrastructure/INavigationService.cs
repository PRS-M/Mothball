namespace MothballMobile.Infrastructure;

/// <summary>
/// Provides navigation services for the mobile application.
/// Abstracts Shell navigation functionality to enable testing and dependency injection.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    /// <param name="route">The route to navigate to.</param>
    /// <returns>A task representing the navigation operation.</returns>
    Task GoToAsync(string route);

    /// <summary>
    /// Navigates to the specified route with parameters.
    /// </summary>
    /// <param name="route">The route to navigate to.</param>
    /// <param name="parameters">Parameters to pass to the destination page.</param>
    /// <returns>A task representing the navigation operation.</returns>
    Task GoToAsync(string route, IDictionary<string, object> parameters);

    /// <summary>
    /// Navigates back to the previous page in the navigation stack.
    /// </summary>
    /// <returns>A task representing the navigation operation.</returns>
    Task GoBackAsync();
}
