namespace MothballMobile.UI.Features.Tags.TagsList;

public partial class TagsListPage : BasePage
{
    public TagsListPage(TagsListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
