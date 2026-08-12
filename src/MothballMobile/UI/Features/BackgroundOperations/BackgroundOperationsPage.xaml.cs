using MothballMobile.Infrastructure;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.BackgroundOperations;

public partial class BackgroundOperationsPage : BasePage
{
    public BackgroundOperationsPage(IPhotoBackgroundOperationTracker tracker)
    {
        InitializeComponent();
        BindingContext = tracker;
    }
}
