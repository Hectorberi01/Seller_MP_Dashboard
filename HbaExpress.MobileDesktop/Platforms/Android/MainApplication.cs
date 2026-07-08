using System;
using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace HbaExpress.MobileDesktop;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    // L'init Firebase est faite dans MauiProgram (ConfigureLifecycleEvents).
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
