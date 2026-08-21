using System.Windows.Input;

namespace MothballMobile.UI.Controls;

public partial class SegmentedSwitch : ContentView
{
    public SegmentedSwitch()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty LeftTextProperty =
        BindableProperty.Create(nameof(LeftText), typeof(string), typeof(SegmentedSwitch), string.Empty);

    public static readonly BindableProperty RightTextProperty =
        BindableProperty.Create(nameof(RightText), typeof(string), typeof(SegmentedSwitch), string.Empty);

    public static readonly BindableProperty IsRightSelectedProperty =
        BindableProperty.Create(nameof(IsRightSelected), typeof(bool), typeof(SegmentedSwitch), false, BindingMode.TwoWay);

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
}
