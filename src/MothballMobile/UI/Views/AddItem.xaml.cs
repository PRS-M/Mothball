using MothballMobile.UI.ViewModels;

namespace MothballMobile.UI.Views;

public partial class AddItem : ContentPage
{
	public AddItem(AddItemViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}