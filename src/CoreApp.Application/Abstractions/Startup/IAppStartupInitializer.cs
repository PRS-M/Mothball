namespace CoreApp.Application.Abstractions.Startup;

/// <summary>
/// Initializes the app's active persistence layer at startup.
/// </summary>
public interface IAppStartupInitializer
{
    /// <summary>
    /// Initializes the active persistence layer.
    /// </summary>
    Task InitializeAsync();
}
