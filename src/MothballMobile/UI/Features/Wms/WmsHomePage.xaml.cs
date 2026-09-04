namespace MothballMobile.UI.Features.Wms;

public partial class WmsHomePage : BasePage
{
    public WmsHomePage(WmsHomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
