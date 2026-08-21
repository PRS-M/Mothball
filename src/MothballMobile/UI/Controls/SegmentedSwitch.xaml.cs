using System.Windows.Input;

namespace MothballMobile.UI.Controls;

public partial class SegmentedSwitch : ContentView
{
    private bool isAnimating;

    public SegmentedSwitch()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        Loaded += OnLoaded;
    }

    public static readonly BindableProperty LeftTextProperty =
        BindableProperty.Create(nameof(LeftText), typeof(string), typeof(SegmentedSwitch), string.Empty);

    public static readonly BindableProperty RightTextProperty =
        BindableProperty.Create(nameof(RightText), typeof(string), typeof(SegmentedSwitch), string.Empty);

    public static readonly BindableProperty IsRightSelectedProperty =
        BindableProperty.Create(
            nameof(IsRightSelected),
            typeof(bool),
            typeof(SegmentedSwitch),
            false,
            BindingMode.TwoWay,
            propertyChanged: OnSelectionChanged);

    public static readonly BindableProperty LeftCommandProperty =
        BindableProperty.Create(nameof(LeftCommand), typeof(ICommand), typeof(SegmentedSwitch));

    public static readonly BindableProperty RightCommandProperty =
        BindableProperty.Create(nameof(RightCommand), typeof(ICommand), typeof(SegmentedSwitch));

    public string LeftText
    {
        get => (string)GetValue(LeftTextProperty);
        set => SetValue(LeftTextProperty, value);
    }

    public string RightText
    {
        get => (string)GetValue(RightTextProperty);
        set => SetValue(RightTextProperty, value);
    }

    public bool IsRightSelected
    {
        get => (bool)GetValue(IsRightSelectedProperty);
        set => SetValue(IsRightSelectedProperty, value);
    }

    public ICommand? LeftCommand
    {
        get => (ICommand?)GetValue(LeftCommandProperty);
        set => SetValue(LeftCommandProperty, value);
    }

    public ICommand? RightCommand
    {
        get => (ICommand?)GetValue(RightCommandProperty);
        set => SetValue(RightCommandProperty, value);
    }

    private static void OnSelectionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SegmentedSwitch segmentedSwitch)
        {
            return;
        }

        if (segmentedSwitch.IsLoaded)
        {
            _ = segmentedSwitch.AnimateSelectionAsync((bool)newValue);
        }
        else
        {
            segmentedSwitch.UpdateIndicatorLayout();
        }
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        UpdateIndicatorLayout();
        Dispatcher.Dispatch(UpdateIndicatorLayout);
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        UpdateIndicatorLayout();
    }

    private void OnSegmentGridSizeChanged(object? sender, EventArgs e)
    {
        UpdateIndicatorLayout();
    }

    private void UpdateIndicatorLayout()
    {
        var segmentWidth = segmentGrid.Width / 2;
        if (segmentWidth <= 0)
        {
            return;
        }

        if (!isAnimating)
        {
            selectionIndicator.TranslationX = IsRightSelected ? segmentWidth : 0;
        }
    }

    private async Task AnimateSelectionAsync(bool isRightSelected)
    {
        var segmentWidth = segmentGrid.Width / 2;
        if (segmentWidth <= 0)
        {
            return;
        }

        var targetTranslation = isRightSelected ? segmentWidth : 0;
        isAnimating = true;

        try
        {
            await Task.WhenAll(
                selectionIndicator.ScaleToAsync(0.96, 1, Easing.Linear),
                selectionIndicator.TranslateToAsync(targetTranslation, 0, 180, Easing.CubicOut));

            await selectionIndicator.ScaleToAsync(1, 120, Easing.CubicOut);
        }
        finally
        {
            isAnimating = false;
            selectionIndicator.TranslationX = targetTranslation;
            selectionIndicator.Scale = 1;
        }
    }

    private void OnLeftTapped(object? sender, TappedEventArgs e)
        => ExecuteCommand(LeftCommand);

    private void OnRightTapped(object? sender, TappedEventArgs e)
        => ExecuteCommand(RightCommand);

    private static void ExecuteCommand(ICommand? command)
    {
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }
    }
}
