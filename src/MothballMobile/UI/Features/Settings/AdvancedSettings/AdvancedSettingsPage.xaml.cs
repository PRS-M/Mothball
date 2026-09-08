namespace MothballMobile.UI.Features.Settings;

public partial class AdvancedSettingsPage : BasePage
{
    public AdvancedSettingsPage(AdvancedSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
