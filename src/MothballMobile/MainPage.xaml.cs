namespace MothballMobile;

public partial class MainPage : BasePage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnContainersClicked(object? sender, EventArgs e)
	{
#if IOS || MACCATALYST
		await Shell.Current.GoToAsync($"//{Infrastructure.NavigationRoutes.TopLevel.MainTabs}/{Infrastructure.NavigationRoutes.TopLevel.Containers}/{Infrastructure.NavigationRoutes.TopLevel.ContainersPage}");
#else
		await Shell.Current.GoToAsync(Infrastructure.NavigationRoutes.HomeContainers);
#endif
	}

	private async void OnItemsClicked(object? sender, EventArgs e)
	{
#if IOS || MACCATALYST
		await Shell.Current.GoToAsync($"//{Infrastructure.NavigationRoutes.TopLevel.MainTabs}/{Infrastructure.NavigationRoutes.TopLevel.Items}/{Infrastructure.NavigationRoutes.TopLevel.ItemsPage}");
#else
		await Shell.Current.GoToAsync(Infrastructure.NavigationRoutes.HomeItems);
#endif
	}

	private async void OnSettingsClicked(object? sender, EventArgs e)
	{
#if IOS || MACCATALYST
		await Shell.Current.GoToAsync($"//{Infrastructure.NavigationRoutes.TopLevel.MainTabs}/{Infrastructure.NavigationRoutes.TopLevel.Settings}/{Infrastructure.NavigationRoutes.TopLevel.SettingsPage}");
#else
		await Shell.Current.GoToAsync(Infrastructure.NavigationRoutes.Settings);
#endif
	}

	private async void OnTagsClicked(object? sender, EventArgs e)
	{
#if IOS || MACCATALYST
		await Shell.Current.GoToAsync($"//{Infrastructure.NavigationRoutes.TopLevel.MainTabs}/{Infrastructure.NavigationRoutes.TopLevel.Tags}/{Infrastructure.NavigationRoutes.TopLevel.TagsPage}");
#else
		await Shell.Current.GoToAsync(Infrastructure.NavigationRoutes.Tags);
#endif
	}
}
