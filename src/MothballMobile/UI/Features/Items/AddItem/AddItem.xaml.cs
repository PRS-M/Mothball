using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class AddItem : BasePage
{
	public AddItem(AddItemViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}