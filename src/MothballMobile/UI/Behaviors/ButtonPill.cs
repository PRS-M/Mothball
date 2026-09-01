namespace MothballMobile.UI.Behaviors;

public static class ButtonPill
{
    public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached(
        "IsEnabled",
        typeof(bool),
        typeof(ButtonPill),
        false,
        propertyChanged: OnIsEnabledChanged);

    public static bool GetIsEnabled(BindableObject bindable)
        => (bool)bindable.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(BindableObject bindable, bool value)
        => bindable.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Button button)
        {
            return;
        }

        if ((bool)oldValue)
        {
            button.SizeChanged -= OnButtonSizeChanged;
        }

        if ((bool)newValue)
        {
            button.SizeChanged += OnButtonSizeChanged;
            UpdateCornerRadius(button);
        }
    }

    private static void OnButtonSizeChanged(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            UpdateCornerRadius(button);
        }
    }

    private static void UpdateCornerRadius(Button button)
    {
        if (button.Height > 0)
        {
            button.CornerRadius = (int)Math.Round(button.Height / 2, MidpointRounding.AwayFromZero);
        }
    }
}
