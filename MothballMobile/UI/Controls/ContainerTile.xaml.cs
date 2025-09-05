using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace MothballMobile.UI.Controls;

public partial class ContainerTile : ContentView
{
	public ContainerTile()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ContainerTile), null);

	public static readonly BindableProperty NameProperty =
		BindableProperty.Create(nameof(Name), typeof(string), typeof(ContainerTile), "Container Name");

	public static readonly BindableProperty NotesProperty =
		BindableProperty.Create(nameof(Notes), typeof(string), typeof(ContainerTile), "Container Description");

	public static readonly BindableProperty ItemCountProperty =
		BindableProperty.Create(nameof(ItemCount), typeof(int), typeof(ContainerTile), 99);

	public static readonly BindableProperty ImageSourceProperty =
		BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(ContainerTile), default(ImageSource));

	public ICommand Command
	{
		get => (ICommand)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	public string Notes
	{
		get => (string)GetValue(NotesProperty);
		set => SetValue(NotesProperty, value);
	}

	public string Name
	{
		get => (string)GetValue(NameProperty);
		set => SetValue(NameProperty, value);
	}

	public int ItemCount
	{
		get => (int)GetValue(ItemCountProperty);
		set => SetValue(ItemCountProperty, value);
	}

	public ImageSource ImageSource
	{
		get => (ImageSource)GetValue(ImageSourceProperty);
		set => SetValue(ImageSourceProperty, value);
	}

	private void Border_OnTapped(object sender, EventArgs e)
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

	static void Border_PointerPressed(object sender, PointerEventArgs e)
	{
		if (sender is Border border)
			VisualStateManager.GoToState(border, "Pressed");
	}

	static void Border_PointerReleased(object sender, PointerEventArgs e)
	{
		if (sender is Border border)
			VisualStateManager.GoToState(border, "Normal");
	}
}