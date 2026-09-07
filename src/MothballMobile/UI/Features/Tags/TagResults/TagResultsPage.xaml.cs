namespace MothballMobile.UI.Features.Tags.TagResults;

public partial class TagResultsPage : BasePage
{
    public TagResultsPage(TagResultsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
