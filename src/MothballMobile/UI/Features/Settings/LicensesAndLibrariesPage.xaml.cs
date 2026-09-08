namespace MothballMobile.UI.Features.Settings;

public partial class LicensesAndLibrariesPage : BasePage
{
    public LicensesAndLibrariesPage(LicensesAndLibrariesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
