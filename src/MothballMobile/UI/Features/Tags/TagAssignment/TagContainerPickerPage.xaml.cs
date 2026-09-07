namespace MothballMobile.UI.Features.Tags.TagAssignment;

public partial class TagContainerPickerPage : BasePage
{
    /// <summary>Creates the existing-container picker page.</summary>
    /// <param name="viewModel">The picker view model resolved by dependency injection.</param>
    public TagContainerPickerPage(TagContainerPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
