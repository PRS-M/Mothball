namespace MothballMobile.Infrastructure.Startup;

/// <summary>
/// Describes progress reported while the application is starting.
/// </summary>
/// <param name="OverallFraction">Overall startup progress in the range 0 to 1.</param>
/// <param name="StepFraction">Current-step progress in the range 0 to 1.</param>
/// <param name="Status">Localization key for the current startup step.</param>
public readonly record struct StartupProgress(
    double OverallFraction,
    double StepFraction,
    string Status);

/// <summary>
/// Defines the coordinated application startup workflow.
/// </summary>
public interface IAppStartupOrchestrator
{
    /// <summary>
    /// Runs the coordinated application startup workflow.
    /// </summary>
    Task StartAsync(IProgress<StartupProgress>? progress = null);
}
