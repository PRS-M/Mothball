using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class AddContainer : BasePage
{
	public AddContainer(AddContainerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}