namespace MothballMobile.Infrastructure.Utilities;

/// <summary>
/// Defines asynchronous initialization for a component.
/// </summary>
public interface IInitializable
{
    /// <summary>
    /// Performs asynchronous initialization.
    /// </summary>
    Task InitializeAsync();
}
