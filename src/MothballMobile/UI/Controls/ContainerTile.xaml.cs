using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace MothballMobile.UI.Controls;

public partial class ContainerTile : ContentView
{
	public ContainerTile()
	{
		InitializeComponent();

		// Ensure the collection exists to allow XAML binding immediately
		ImagePaths = new ObservableCollection<string>();
	}

	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ContainerTile), null);

	public static readonly BindableProperty NameProperty =
		BindableProperty.Create(nameof(Name), typeof(string), typeof(ContainerTile), "Container Name");

	public static readonly BindableProperty NotesProperty =
		BindableProperty.Create(nameof(Notes), typeof(string), typeof(ContainerTile), "Container Description");

	public static readonly BindableProperty ItemCountProperty =
		BindableProperty.Create(nameof(ItemCount), typeof(string), typeof(ContainerTile), "Items Count: 99");

	public static readonly BindableProperty ImagePathsProperty =
		BindableProperty.Create(nameof(ImagePaths), typeof(ObservableCollection<string>), typeof(ContainerTile), default(ObservableCollection<string>));

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

	public string ItemCount
	{
		get => (string)GetValue(ItemCountProperty);
		set => SetValue(ItemCountProperty, value);
	}

	public ObservableCollection<string> ImagePaths
	{
		get => (ObservableCollection<string>)GetValue(ImagePathsProperty);
		set => SetValue(ImagePathsProperty, value);
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