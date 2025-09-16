using System;
using System.Threading.Tasks;

namespace MothballMobile.Infrastructure;

public interface IRetryService
{
    Task<bool> RetryAsync(
        Func<Task<bool>> attempt,
        string canceledTitle,
        string canceledMessage,
        string retryButton,
        string continueButton,
        string? continueAlertTitle = null,
        string? continueAlertMessage = null);
}
