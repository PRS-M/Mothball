using System.Diagnostics;

namespace MothballMobile.Infrastructure;

public static class TaskExtensions
{
    public static void Forget(this Task task)
    {
        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        _ = ObserveAsync(task);
    }

    private static async Task ObserveAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unobserved task failure: {ex}");
        }
    }
}