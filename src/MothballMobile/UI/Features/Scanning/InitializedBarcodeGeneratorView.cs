using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace MothballMobile.UI.Features.Scanning;

/// <summary>
/// Initializes the ZXing barcode generator with a valid format before its native handler attaches.
/// </summary>
public sealed class InitializedBarcodeGeneratorView : BarcodeGeneratorView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InitializedBarcodeGeneratorView"/> class.
    /// </summary>
    public InitializedBarcodeGeneratorView()
    {
        Format = BarcodeFormat.QrCode;
    }
}
