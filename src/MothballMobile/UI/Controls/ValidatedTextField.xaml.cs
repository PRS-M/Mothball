using System.Windows.Input;

namespace MothballMobile.UI.Controls;

public partial class ValidatedTextField : ContentView
{
	public ValidatedTextField()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty LabelProperty =
		BindableProperty.Create(nameof(Label), typeof(string), typeof(ValidatedTextField), string.Empty);

	public static readonly BindableProperty PlaceholderProperty =
		BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(ValidatedTextField), string.Empty);

	public static readonly BindableProperty TextProperty =
		BindableProperty.Create(nameof(Text), typeof(string), typeof(ValidatedTextField), string.Empty, defaultBindingMode: BindingMode.TwoWay);

	public static readonly BindableProperty ValidationMessageProperty =
		BindableProperty.Create(nameof(ValidationMessage), typeof(string), typeof(ValidatedTextField), string.Empty,
			propertyChanged: static (bindable, _, __) =>
			{
				if (bindable is ValidatedTextField field)
					field.OnPropertyChanged(nameof(HasValidationMessage));
			});

	public static readonly BindableProperty LabelMarginProperty =
		BindableProperty.Create(nameof(LabelMargin), typeof(Thickness), typeof(ValidatedTextField), new Thickness(4, 0));

	public static readonly BindableProperty KeyboardProperty =
		BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(ValidatedTextField), Keyboard.Default);

	public static readonly BindableProperty ReturnTypeProperty =
		BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(ValidatedTextField), ReturnType.Default);

	public static readonly BindableProperty ReturnCommandProperty =
		BindableProperty.Create(nameof(ReturnCommand), typeof(ICommand), typeof(ValidatedTextField), null);

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

	public string ValidationMessage
	{
		get => (string)GetValue(ValidationMessageProperty);
		set => SetValue(ValidationMessageProperty, value);
	}

	public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

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
}
