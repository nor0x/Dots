using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace Dots;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

#if WINDOWS
        AppCenter.Start(
          "windowsdesktop=a85c1cd2-52b0-4524-addd-e37def4d5290;" +
          "macos=5a092ef1-f7ee-4161-b571-bae510ec1572;",
          typeof(Analytics), typeof(Crashes));

#endif

        MainPage = new AppShell();
	}
}
