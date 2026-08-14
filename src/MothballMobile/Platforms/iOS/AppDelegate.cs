using Foundation;

namespace MothballMobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp()
	{
		var app = MauiProgram.CreateMauiApp();
#if IOS
		Google.MobileAds.MobileAds.SharedInstance.Start(completionHandler: null);
#endif
		return app;
	}
}
