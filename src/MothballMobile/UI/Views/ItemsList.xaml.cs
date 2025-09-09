using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class ItemsList : ContentPage
{
	private ItemsListViewModel ViewModel => (ItemsListViewModel)BindingContext;

	public ItemsList(ItemsListViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await ViewModel.InitializeAsync();
	}
}