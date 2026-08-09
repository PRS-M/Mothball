namespace MothballMobile;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnContainersClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(Infrastructure.NavigationRoutes.HomeContainers);
	}

	private async void OnItemsClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(Infrastructure.NavigationRoutes.HomeItems);
	}

	private async void OnSettingsClicked(object? sender, EventArgs e)
	{
		await DisplayAlertAsync("Settings", "Settings will be implemented in a later update.", "OK");
	}
}
