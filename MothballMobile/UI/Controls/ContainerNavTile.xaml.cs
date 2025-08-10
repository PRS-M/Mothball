using System.Windows.Input;

namespace MothballMobile.UI.Controls;

public partial class ContainerNavTile : ContentView
{
	public ContainerNavTile()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ContainerNavTile), null);

	public static readonly BindableProperty TextProperty =
		BindableProperty.Create(nameof(Text), typeof(string), typeof(ContainerNavTile), string.Empty);

	public ICommand Command
	{
		get => (ICommand)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	private void CardBorder_OnTapped(object sender, EventArgs e)
	{
		if (sender is Border border)
			VisualStateManager.GoToState(border, "Pressed");

		if (Command != null && Command.CanExecute(null))
		{
			Command.Execute(null);
		}

		Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () =>
		{
			if (sender is Border border)
				VisualStateManager.GoToState(border, "Normal");
			return false;
		});
	}

	static void CardBorder_PointerPressed(object sender, PointerEventArgs e)
	{
		if (sender is Border border)
			VisualStateManager.GoToState(border, "Pressed");
	}

	static void CardBorder_PointerReleased(object sender, PointerEventArgs e)
	{
		if (sender is Border border)
			VisualStateManager.GoToState(border, "Normal");
	}
}