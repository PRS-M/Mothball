namespace CoreApp.Abstractions.Startup;

/// <summary>
/// Initializes the app's active persistence layer at startup.
/// </summary>
public interface IAppStartupInitializer
{
    Task InitializeAsync();
}
