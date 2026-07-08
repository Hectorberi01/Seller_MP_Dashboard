using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace HbaExpress.MobileDesktop;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    // L'init Firebase est faite dans MauiProgram (ConfigureLifecycleEvents).
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
