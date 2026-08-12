using System.Collections;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace MothballMobile.UI.Controls;

public partial class ItemTile : ContentView
{
	public ItemTile()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ItemTile), null);

	public static readonly BindableProperty NameProperty =
		BindableProperty.Create(nameof(Name), typeof(string), typeof(ItemTile), string.Empty);

	public static readonly BindableProperty DescriptionProperty =
		BindableProperty.Create(nameof(Description), typeof(string), typeof(ItemTile), string.Empty);

	public static readonly BindableProperty QuantityProperty =
		BindableProperty.Create(nameof(Quantity), typeof(int), typeof(ItemTile), 0);

	public static readonly BindableProperty ShowQuantityProperty =
		BindableProperty.Create(nameof(ShowQuantity), typeof(bool), typeof(ItemTile), false);

	public static readonly BindableProperty ImagePathsProperty =
		BindableProperty.Create(nameof(ImagePaths), typeof(IEnumerable), typeof(ItemTile), default(IEnumerable));

	public static readonly BindableProperty ImageSizeProperty =
		BindableProperty.Create(nameof(ImageSize), typeof(double), typeof(ItemTile), 72d);

	public static readonly BindableProperty ImageCornerRadiusProperty =
		BindableProperty.Create(nameof(ImageCornerRadius), typeof(float), typeof(ItemTile), 10f);

	public ICommand? Command
	{
		get => (ICommand?)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	public string Name
	{
		get => (string)GetValue(NameProperty);
		set => SetValue(NameProperty, value);
	}

	public string Description
	{
		get => (string)GetValue(DescriptionProperty);
		set => SetValue(DescriptionProperty, value);
	}

	public int Quantity
	{
		get => (int)GetValue(QuantityProperty);
		set => SetValue(QuantityProperty, value);
	}

	public bool ShowQuantity
	{
		get => (bool)GetValue(ShowQuantityProperty);
		set => SetValue(ShowQuantityProperty, value);
	}

	public IEnumerable? ImagePaths
	{
		get => (IEnumerable?)GetValue(ImagePathsProperty);
		set => SetValue(ImagePathsProperty, value);
	}

	public double ImageSize
	{
		get => (double)GetValue(ImageSizeProperty);
		set => SetValue(ImageSizeProperty, value);
	}

	public float ImageCornerRadius
	{
		get => (float)GetValue(ImageCornerRadiusProperty);
		set => SetValue(ImageCornerRadiusProperty, value);
	}

	private void Border_OnTapped(object sender, EventArgs e)
	{
		if (sender is Border border)
			VisualStateManager.GoToState(border, "Pressed");

		if (Command is not null && Command.CanExecute(null))
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

	private static void Border_PointerPressed(object sender, PointerEventArgs e)
	{
		if (sender is Border border)
			VisualStateManager.GoToState(border, "Pressed");
	}

	private static void Border_PointerReleased(object sender, PointerEventArgs e)
	{
		if (sender is Border border)
			VisualStateManager.GoToState(border, "Normal");
	}
}
