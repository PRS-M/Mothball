using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class ContainersList : ContentPage
{
	private ContainerListViewModel ViewModel => BindingContext as ContainerListViewModel;

	public ContainersList()
	{
		InitializeComponent();
		BindingContext = new MothballMobile.UI.ViewModels.ContainerListViewModel(
			new CoreApp.Services.Implementations.ContainerJsonHandler(
				new CoreApp.Services.Implementations.JsonHandler(
					new MothballMobile.Core.Services.MobileFileHandler(Microsoft.Maui.Storage.FileSystem.Current)
				)
			),
			new MothballMobile.Core.Services.MobileFileHandler(Microsoft.Maui.Storage.FileSystem.Current)
		);
		this.Loaded += ContainersList_Loaded;
	}

	private async void ContainersList_Loaded(object? sender, EventArgs e)
	{
		if (ViewModel != null)
		{
			await ViewModel.InitializeAsync();
		}
	}

	private async void OnRemainingItemsThresholdReached(object sender, EventArgs e)
	{
		if (ViewModel != null)
		{
			await ViewModel.LoadNextPageAsync();
		}
	}
}