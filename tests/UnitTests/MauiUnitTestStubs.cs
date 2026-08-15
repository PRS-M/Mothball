global using Microsoft.Maui.ApplicationModel;
global using Microsoft.Maui.Controls;
global using MothballMobile.UI.Shared;

namespace Microsoft.Maui.ApplicationModel
{
    public static class MainThread
    {
        public static Task InvokeOnMainThreadAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public static Task<T> InvokeOnMainThreadAsync<T>(Func<T> action)
            => Task.FromResult(action());

        public static Task InvokeOnMainThreadAsync(Func<Task> action)
            => action();

        public static Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> action)
            => action();

        public static void BeginInvokeOnMainThread(Action action)
            => action();
    }
}

namespace Microsoft.Maui.Controls
{
    public interface IQueryAttributable
    {
        void ApplyQueryAttributes(IDictionary<string, object> query);
    }
}
