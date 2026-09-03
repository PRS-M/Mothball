namespace MothballMobile.UI.Features.Containers.AddContainer;

public partial class AddContainerPage : BasePage
{
	public AddContainerPage(AddContainerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}