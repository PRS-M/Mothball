using System.Windows.Input;

namespace MothballMobile.UI.Controls;

public partial class LabeledEntryField : ContentView
{
	public LabeledEntryField()
	{
		InitializeComponent();
		ApplyWrapperVisibility();
	}

	public static readonly BindableProperty LabelProperty =
		BindableProperty.Create(nameof(Label), typeof(string), typeof(LabeledEntryField), string.Empty);

	public static readonly BindableProperty PlaceholderProperty =
		BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(LabeledEntryField), string.Empty);

	public static readonly BindableProperty TextProperty =
		BindableProperty.Create(nameof(Text), typeof(string), typeof(LabeledEntryField), string.Empty, defaultBindingMode: BindingMode.TwoWay);

	public static readonly BindableProperty LabelMarginProperty =
		BindableProperty.Create(nameof(LabelMargin), typeof(Thickness), typeof(LabeledEntryField), new Thickness(4, 0));

	public static readonly BindableProperty KeyboardProperty =
		BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(LabeledEntryField), Keyboard.Default);

	public static readonly BindableProperty ReturnTypeProperty =
		BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(LabeledEntryField), ReturnType.Default);

	public static readonly BindableProperty ReturnCommandProperty =
		BindableProperty.Create(nameof(ReturnCommand), typeof(ICommand), typeof(LabeledEntryField), null);

	public static readonly BindableProperty UnfocusedCommandProperty =
		BindableProperty.Create(nameof(UnfocusedCommand), typeof(ICommand), typeof(LabeledEntryField), null);

	public static readonly BindableProperty FieldWidthRequestProperty =
		BindableProperty.Create(nameof(FieldWidthRequest), typeof(double), typeof(LabeledEntryField), -1d);

	public static readonly BindableProperty WrapperStyleProperty =
		BindableProperty.Create(nameof(WrapperStyle), typeof(Style), typeof(LabeledEntryField), null);

	public static readonly BindableProperty UseWrapperProperty =
		BindableProperty.Create(nameof(UseWrapper), typeof(bool), typeof(LabeledEntryField), true,
			propertyChanged: static (bindable, _, __) =>
			{
				if (bindable is LabeledEntryField field)
					field.ApplyWrapperVisibility();
			});

	public string Label
	{
		get => (string)GetValue(LabelProperty);
		set => SetValue(LabelProperty, value);
	}

	public string Placeholder
	{
		get => (string)GetValue(PlaceholderProperty);
		set => SetValue(PlaceholderProperty, value);
	}

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public Thickness LabelMargin
	{
		get => (Thickness)GetValue(LabelMarginProperty);
		set => SetValue(LabelMarginProperty, value);
	}

	public Keyboard Keyboard
	{
		get => (Keyboard)GetValue(KeyboardProperty);
		set => SetValue(KeyboardProperty, value);
	}

	public ReturnType ReturnType
	{
		get => (ReturnType)GetValue(ReturnTypeProperty);
		set => SetValue(ReturnTypeProperty, value);
	}

	public ICommand? ReturnCommand
	{
		get => (ICommand?)GetValue(ReturnCommandProperty);
		set => SetValue(ReturnCommandProperty, value);
	}

	public ICommand? UnfocusedCommand
	{
		get => (ICommand?)GetValue(UnfocusedCommandProperty);
		set => SetValue(UnfocusedCommandProperty, value);
	}

	public double FieldWidthRequest
	{
		get => (double)GetValue(FieldWidthRequestProperty);
		set => SetValue(FieldWidthRequestProperty, value);
	}

	public Style? WrapperStyle
	{
		get => (Style?)GetValue(WrapperStyleProperty);
		set => SetValue(WrapperStyleProperty, value);
	}

	public bool UseWrapper
	{
		get => (bool)GetValue(UseWrapperProperty);
		set => SetValue(UseWrapperProperty, value);
	}

	private void OnEntryUnfocused(object? sender, FocusEventArgs e)
	{
		if (UnfocusedCommand?.CanExecute(null) == true)
		{
			UnfocusedCommand.Execute(null);
		}
	}

	void ApplyWrapperVisibility()
	{
		WrapperBorder.IsVisible = UseWrapper;
		NakedEntry.IsVisible = !UseWrapper;
	}
}
