using CoreApp.Interfaces;

namespace MothballMobile.UI.Shared;

public static class PhotoSourceSelector
{
    public static async Task<PhotoSource?> SelectPhotoSourceAsync(IPopupService popup, IPopupDefinitionService popupDefinitions)
    {
        ArgumentNullException.ThrowIfNull(popup);
        ArgumentNullException.ThrowIfNull(popupDefinitions);

        return await popup.SelectValueOptionAsync(popupDefinitions.PhotoSourcePicker());
    }
}
