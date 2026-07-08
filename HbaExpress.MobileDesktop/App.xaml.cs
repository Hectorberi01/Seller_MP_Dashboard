using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace HbaExpress.MobileDesktop;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(new MainPage()) { Title = "HbaExpress Pro" };
}
