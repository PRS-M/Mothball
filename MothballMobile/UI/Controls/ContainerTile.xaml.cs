namespace MothballMobile.UI.Controls;

public partial class ContainerTile : ContentView
{
	public ContainerTile()
	{
		InitializeComponent();
	}

	public static readonly BindableProperty NameProperty =
		BindableProperty.Create(nameof(Name), typeof(string), typeof(ContainerTile), "Container Name");

	public static readonly BindableProperty DescriptionProperty =
		BindableProperty.Create(nameof(Description), typeof(string), typeof(ContainerTile), "Container Description");

	public static readonly BindableProperty LocationDescriptionProperty =
		BindableProperty.Create(nameof(LocationDescription), typeof(string), typeof(ContainerTile), "Location Description");

	public static readonly BindableProperty ItemCountProperty =
		BindableProperty.Create(nameof(ItemCount), typeof(int), typeof(ContainerTile), 99);

	public string Description
	{
		get => (string)GetValue(DescriptionProperty);
		set => SetValue(DescriptionProperty, value);
	}

	public string Name
	{
		get => (string)GetValue(NameProperty);
		set => SetValue(NameProperty, value);
	}

	public string LocationDescription
	{
		get => (string)GetValue(LocationDescriptionProperty);
		set => SetValue(LocationDescriptionProperty, value);
	}

	public int ItemCount
	{
		get => (int)GetValue(ItemCountProperty);
		set => SetValue(ItemCountProperty, value);
	}
}