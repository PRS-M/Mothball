using CoreApp.Interfaces;
using MothballMobile.Infrastructure;

namespace MothballMobile.UI.Shared;

public static class PhotoSourceSelector
{
    public static async Task<PhotoSource?> SelectPhotoSourceAsync(IPopupService popup)
    {
        ArgumentNullException.ThrowIfNull(popup);

        const string selectPhoto = "Select Photo";
        const string capturePhoto = "Capture Photo";

        var selected = await popup.SelectOptionAsync("Add photo", "Cancel", selectPhoto, capturePhoto);
        return selected switch
        {
            selectPhoto => PhotoSource.Library,
            capturePhoto => PhotoSource.Camera,
            _ => null
        };
    }
}
