using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Contracts.Tags;

namespace MothballMobile.UI.Features.Tags.TagsList;

/// <summary>
/// Presents one tag and its current assignment counts.
/// </summary>
public partial class TagViewModel : ObservableObject
{
    private readonly INavigationService navigation;

    public TagViewModel(TagUsageSummary summary, INavigationService navigation)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    public TagUsageSummary Summary { get; }
    public string Name => $"#{Summary.Name}";
    public string UsageText => LocalizationManager.Current.Format(
        "TagUsageFormat",
        Summary.ItemCount,
        Summary.ContainerCount);

    [RelayCommand]
    private Task OpenAsync()
        => navigation.GoToAsync(
            NavigationRoutes.TagResults,
            new Infrastructure.Navigation.TagResultsNavigationRequest(Summary.TagId, Summary.Name));
}
