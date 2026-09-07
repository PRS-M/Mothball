namespace MothballMobile.UI.Features.Tags.TagAssignment;

public partial class TagItemPickerPage : BasePage
{
    /// <summary>Creates the existing-item picker page.</summary>
    /// <param name="viewModel">The picker view model resolved by dependency injection.</param>
    public TagItemPickerPage(TagItemPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
