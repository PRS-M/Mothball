global using Microsoft.Maui.ApplicationModel;
global using Microsoft.Maui.Controls;

namespace Microsoft.Maui.ApplicationModel
{
    public enum AppTheme
    {
        Unspecified,
        Light,
        Dark
    }

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

namespace Microsoft.Maui.Devices
{
    public readonly record struct DevicePlatform(string Platform)
    {
        public static DevicePlatform Android { get; } = new(nameof(Android));

        public static DevicePlatform iOS { get; } = new(nameof(iOS));

        public static DevicePlatform MacCatalyst { get; } = new(nameof(MacCatalyst));

        public static DevicePlatform Tizen { get; } = new(nameof(Tizen));

        public static DevicePlatform WinUI { get; } = new(nameof(WinUI));
    }
}

namespace Microsoft.Maui.Controls
{
    public class Application
    {
        public static Application? Current { get; set; }

        public AppTheme UserAppTheme { get; set; }
    }

    public interface IQueryAttributable
    {
        void ApplyQueryAttributes(IDictionary<string, object> query);
    }
}

namespace Microsoft.Maui.Storage
{
    public interface IFilePicker
    {
        Task<FileResult?> PickAsync(PickOptions? options = null);
    }

    public interface ISecureStorage
    {
        Task<string?> GetAsync(string key);

        Task SetAsync(string key, string value);
    }

    public sealed class PickOptions
    {
        public string? PickerTitle { get; set; }

        public FilePickerFileType? FileTypes { get; set; }
    }

    public sealed class FilePickerFileType
    {
        public FilePickerFileType(IReadOnlyDictionary<Microsoft.Maui.Devices.DevicePlatform, IEnumerable<string>> fileTypes)
        {
            FileTypes = fileTypes;
        }

        public IReadOnlyDictionary<Microsoft.Maui.Devices.DevicePlatform, IEnumerable<string>> FileTypes { get; }
    }

    public class FileResult
    {
        public FileResult(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }

        public virtual Task<Stream> OpenReadAsync()
            => Task.FromResult<Stream>(Stream.Null);
    }
}
