namespace MothballMobile.Infrastructure;

public static class TaskExtensions
{
    public static void FireAndForget(this Task task, IBackgroundTaskObserver observer, string operationName)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(observer);

        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        _ = ObserveAsync(task, observer, operationName);
    }

    private static async Task ObserveAsync(Task task, IBackgroundTaskObserver observer, string operationName)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            observer.OnFailure(operationName, ex);
        }
    }
}
