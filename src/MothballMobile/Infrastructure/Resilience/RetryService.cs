namespace MothballMobile.Infrastructure.Resilience;

public class RetryService : IRetryService
{
    private readonly IPopupService popupService;

    public RetryService(IPopupService popupService)
    {
        this.popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));
    }

    /// <inheritdoc />
    public async Task<bool> RetryAsync(
        Func<Task<bool>> attempt,
        string canceledTitle,
        string canceledMessage,
        string retryButton,
        string continueButton,
        string? continueAlertTitle = null,
        string? continueAlertMessage = null)
    {
        if (attempt == null) throw new ArgumentNullException(nameof(attempt));

        if (await attempt())
        {
            return true;
        }

        bool retry = await popupService.ConfirmAsync(
            canceledTitle,
            canceledMessage,
            retryButton,
            continueButton);

        if (retry)
        {
            return await attempt();
        }

        if (!string.IsNullOrWhiteSpace(continueAlertTitle) || !string.IsNullOrWhiteSpace(continueAlertMessage))
        {
            await popupService.ShowAlertAsync(
                continueAlertTitle ?? string.Empty,
                continueAlertMessage ?? string.Empty);
        }

        return false;
    }
}
