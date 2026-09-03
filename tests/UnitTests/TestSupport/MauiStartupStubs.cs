using Microsoft.Extensions.Logging;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Scanning;

namespace Microsoft.Maui.Controls
{

public class Color
{
    public static Color FromArgb(string value) => new();
}

public class Page
{
    public object? Content { get; set; }
}

public class ContentPage : Page
{
    public Color? BackgroundColor { get; set; }
}

public class Shell : Page
{
    public event EventHandler? Loaded;

    internal void RaiseLoaded()
        => Loaded?.Invoke(this, EventArgs.Empty);
}

public class Window
{
    private Page? page;

    public Window(Page page)
    {
        Page = page;
    }

    public Page? Page
    {
        get => page;
        set
        {
            page = value;
            if (value is Shell shell)
            {
                shell.RaiseLoaded();
            }
        }
    }
}

public class Button
{
    public string? Text { get; set; }

    public bool IsEnabled { get; set; } = true;

    public event EventHandler? Clicked;

    public void RaiseClicked()
        => Clicked?.Invoke(this, EventArgs.Empty);
}

public class Label
{
    public string? Text { get; set; }

    public TextAlignment HorizontalTextAlignment { get; set; }

    public FontAttributes FontAttributes { get; set; }

    public LineBreakMode LineBreakMode { get; set; }
}

public class ActivityIndicator
{
    public bool IsRunning { get; set; }

    public double WidthRequest { get; set; }

    public double HeightRequest { get; set; }
}

public class Thickness
{
    public Thickness(double value)
    {
    }
}

public class VerticalStackLayout
{
    public Thickness? Padding { get; set; }

    public double Spacing { get; set; }

    public LayoutOptions VerticalOptions { get; set; }

    public LayoutOptions HorizontalOptions { get; set; }

    public IList<object> Children { get; } = new List<object>();
}

public enum LayoutOptions
{
    Center
}

public enum TextAlignment
{
    Center
}

public enum LineBreakMode
{
    WordWrap
}

public enum FontAttributes
{
    Bold
}

}

namespace MothballMobile
{

public sealed record AdMobSettings(string AppOpenAdUnitId, string BannerAdUnitId);

public partial class AppShell : Shell
{
    public AppShell(
        IPopupService popup,
        ILogger<AppShell> logger,
        BarcodeLookupCoordinator barcodeLookupCoordinator)
    {
    }
}

}
