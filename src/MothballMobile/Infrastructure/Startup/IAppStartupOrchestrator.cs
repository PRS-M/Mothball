namespace MothballMobile.Infrastructure.Startup;

/// <summary>
/// Defines the coordinated application startup workflow.
/// </summary>
public interface IAppStartupOrchestrator
{
    /// <summary>
    /// Runs the coordinated application startup workflow.
    /// </summary>
    Task StartAsync();
}
