using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class ItemsList : ContentPage
{
	private ItemsListViewModel ViewModel => (ItemsListViewModel)BindingContext;

	public ItemsList(ItemsListViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		this.Loaded += OnLoaded;
	}

	private async void OnLoaded(object? sender, EventArgs e)
	{
		await ViewModel.InitializeAsync();
	}
}