using System;
using System.Threading.Tasks;

namespace MothballMobile.Infrastructure.Resilience;

/// <summary>
/// Defines user-mediated retry behavior for asynchronous operations.
/// </summary>
public interface IRetryService
{
    /// <summary>
    /// Repeats an operation after presenting the user with retry options.
    /// </summary>
    /// <param name="attempt">The operation to retry.</param>
    /// <param name="canceledTitle">The title shown when the operation is cancelled.</param>
    /// <param name="canceledMessage">The message shown when the operation is cancelled.</param>
    /// <param name="retryButton">The label for retrying the operation.</param>
    /// <param name="continueButton">The label for continuing without a successful retry.</param>
    /// <param name="continueAlertTitle">An optional title for the continue alert.</param>
    /// <param name="continueAlertMessage">An optional message for the continue alert.</param>
    Task<bool> RetryAsync(
        Func<Task<bool>> attempt,
        string canceledTitle,
        string canceledMessage,
        string retryButton,
        string continueButton,
        string? continueAlertTitle = null,
        string? continueAlertMessage = null);
}
