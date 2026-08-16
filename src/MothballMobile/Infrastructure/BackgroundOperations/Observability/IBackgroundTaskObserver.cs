namespace MothballMobile.Infrastructure.BackgroundOperations.Observability;

/// <summary>
/// Defines observability hooks for background task failures.
/// </summary>
public interface IBackgroundTaskObserver
{
    /// <summary>
    /// Records a failure from a named background operation.
    /// </summary>
    /// <param name="operationName">The value used by the operation.</param>
    /// <param name="exception">The value used by the operation.</param>
    void OnFailure(string operationName, Exception exception);
}
