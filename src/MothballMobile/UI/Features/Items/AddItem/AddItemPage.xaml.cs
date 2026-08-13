using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Items.AddItem;

public partial class AddItemPage : BasePage
{
	public AddItemPage(AddItemViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}
