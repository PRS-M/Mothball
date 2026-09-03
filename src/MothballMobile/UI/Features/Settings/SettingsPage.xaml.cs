namespace MothballMobile.UI.Features.Settings;

public partial class SettingsPage : BasePage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
